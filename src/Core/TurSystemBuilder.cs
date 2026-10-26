using Microsoft.Extensions.FileSystemGlobbing;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Tur.Core
{
    public class TurSystemBuilder
    {
        private readonly string _rootDir;
        private readonly Matcher _matcher;
        private readonly TurBuildOptions _options;
        private readonly CancellationToken _cancellationToken;
        private readonly Action<string, Exception> _onError;
        private readonly HashSet<string> _visitedDirs;

        public TurSystemBuilder(string rootDir, TurBuildOptions options, CancellationToken cancellationToken, Action<string, Exception> onError = null)
        {
            _rootDir = Path.GetFullPath(rootDir);
            _options = options;
            _matcher = BuildMatcher();
            _cancellationToken = cancellationToken;
            _onError = onError;
            if (_options.FollowLinks)
            {
                _visitedDirs = new HashSet<string>(StringComparer.Ordinal)
                {
                    ResolveRealPath(_rootDir)
                };
            }
        }

        private static string ResolveRealPath(string path)
        {
            try
            {
                var target = new DirectoryInfo(path).ResolveLinkTarget(returnFinalTarget: true);
                return Path.GetFullPath(target == null ? path : target.FullName);
            }
            catch (Exception)
            {
                return Path.GetFullPath(path);
            }
        }

        public IEnumerable<TurFileSystem> Build()
        {
            if (!_options.IncludeFiles && !_options.IncludeDirectories)
            {
                yield break;
            }

            var entry = new TurDirectoryEntry
            {
                FullPath = _rootDir,
                Name = Path.GetFileName(_rootDir)
            };

            if (_options.ScanParallelism > 1)
            {
                BuildParallel(entry);
            }
            else
            {
                BuildSequential(entry, 0);
            }

            foreach (var item in Traverse(entry))
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                if (IsIncluded(item))
                {
                    yield return item;
                }
            }
        }

        private Matcher BuildMatcher()
        {
            if (!_options.IncludeGlobPatterns.Any() && !_options.ExcludeGlobPatterns.Any())
            {
                return null;
            }

            var matcher = new Matcher();
            if (!_options.IncludeGlobPatterns.Any())
            {
                _ = matcher.AddInclude("**");
            }
            else
            {
                matcher.AddIncludePatterns(_options.IncludeGlobPatterns);
            }

            matcher.AddExcludePatterns(_options.ExcludeGlobPatterns);
            return matcher;
        }

        private IEnumerable<TurFileSystem> Traverse(TurDirectoryEntry dirEntry)
        {
            foreach (var dirItem in dirEntry.DirectoryEntries)
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                foreach (var item in Traverse(dirItem))
                {
                    yield return item;
                }

                if (_options.IncludeDirectories)
                {
                    yield return new TurFileSystem
                    {
                        FullPath = dirItem.FullPath,
                        IsDirectory = true,
                        Name = dirItem.Name,
                        RelativePath = dirItem.RelativePath
                    };
                }
            }

            if (_options.IncludeFiles)
            {
                foreach (var fileItem in dirEntry.FileEntries)
                {
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        yield break;
                    }

                    yield return new TurFileSystem
                    {
                        FullPath = fileItem.FullPath,
                        Length = fileItem.Length,
                        Name = fileItem.Name,
                        RelativePath = fileItem.RelativePath,
                        CreationTime = fileItem.CreationTime,
                        LastModifyTime = fileItem.LastModifyTime
                    };
                }
            }
        }

        private void BuildSequential(TurDirectoryEntry dirEntry, int depth)
        {
            foreach (var child in EnumerateDirectory(dirEntry, depth))
            {
                BuildSequential(child, depth + 1);
            }
        }

        /// <summary>
        /// Breadth-first scan with a bounded number of workers fed through a channel.
        /// Chosen over nested Parallel.ForEach because deep trees would compound
        /// parallelism (and risk pool starvation); here concurrency is exactly N.
        /// Each directory's entry lists are touched only by its own worker.
        /// </summary>
        private void BuildParallel(TurDirectoryEntry root)
        {
            var workerCount = Math.Min(Math.Max(_options.ScanParallelism, 2), 64);
            var channel = Channel.CreateUnbounded<(TurDirectoryEntry Entry, int Depth)>(
                new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });

            var pending = 1;
            Exception failure = null;
            _ = channel.Writer.TryWrite((root, 0));

            async Task WorkerAsync()
            {
                try
                {
                    await foreach (var work in channel.Reader.ReadAllAsync(_cancellationToken).ConfigureAwait(false))
                    {
                        if (Volatile.Read(ref failure) != null)
                        {
                            if (Interlocked.Decrement(ref pending) == 0)
                            {
                                channel.Writer.TryComplete();
                            }

                            continue;
                        }

                        try
                        {
                            foreach (var child in EnumerateDirectory(work.Entry, work.Depth))
                            {
                                _ = Interlocked.Increment(ref pending);
                                _ = channel.Writer.TryWrite((child, work.Depth + 1));
                            }
                        }
                        catch (Exception ex)
                        {
                            Interlocked.CompareExchange(ref failure, ex, null);
                            channel.Writer.TryComplete();
                        }
                        finally
                        {
                            if (Interlocked.Decrement(ref pending) == 0)
                            {
                                channel.Writer.TryComplete();
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Cancellation is cooperative; the caller discards the partial tree.
                }
            }

            var workers = new Task[workerCount];
            for (var i = 0; i < workerCount; i++)
            {
                workers[i] = Task.Run(WorkerAsync);
            }

            try
            {
                Task.WhenAll(workers).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) { }

            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        /// <summary>
        /// Enumerates one directory level into dirEntry's lists and returns the child
        /// directories that still need recursion. Failing to open one directory never
        /// affects its siblings: with IgnoreError it is reported and skipped.
        /// </summary>
        private List<TurDirectoryEntry> EnumerateDirectory(TurDirectoryEntry dirEntry, int depth)
        {
            List<TurDirectoryEntry> children = new();

            // Entries of this directory sit at depth + 1; stop before exceeding the limit.
            if (_options.MaxDepth.HasValue && depth >= _options.MaxDepth.Value)
            {
                return children;
            }

            FileSystemEnumerable<TurFileEntry> fileSystemEnumerable;
            try
            {
                fileSystemEnumerable = new FileSystemEnumerable<TurFileEntry>(
                    dirEntry.FullPath,
                    (ref FileSystemEntry entry) =>
                    {
                        var result = entry.IsDirectory ? new TurDirectoryEntry() : new TurFileEntry();
                        result.FullPath = entry.ToFullPath();
                        result.Name = entry.FileName.ToString();
                        // Build the relative path incrementally from the parent instead of
                        // recomputing Path.GetRelativePath (string-heavy) for every entry.
                        result.RelativePath = string.IsNullOrEmpty(dirEntry.RelativePath)
                            ? result.Name
                            : dirEntry.RelativePath + Path.DirectorySeparatorChar + result.Name;
                        if (!entry.IsDirectory)
                        {
                            if (_options.IncludeFileSize)
                            {
                                result.Length = entry.Length;
                            }

                            if (_options.IncludeAttributes)
                            {
                                result.CreationTime = entry.CreationTimeUtc.LocalDateTime;
                                result.LastModifyTime = entry.LastWriteTimeUtc.LocalDateTime;
                            }
                        }

                        return result;
                    },
                    new EnumerationOptions
                    {
                        // Symlinks/junctions are skipped unless explicitly followed;
                        // following them without loop detection could recurse forever.
                        AttributesToSkip = _options.FollowLinks ? FileAttributes.System : FileAttributes.System | FileAttributes.ReparsePoint,
                        IgnoreInaccessible = true,
                        RecurseSubdirectories = false,
                        ReturnSpecialDirectories = false
                    });
            }
            catch (Exception ex)
            {
                if (!_options.IgnoreError)
                {
                    throw;
                }

                _onError?.Invoke(dirEntry.FullPath, ex);
                return children;
            }

            try
            {
                foreach (var item in fileSystemEnumerable)
                {
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    if (item is TurDirectoryEntry dirItem)
                    {
                        if (_options.FollowLinks)
                        {
                            lock (_visitedDirs)
                            {
                                if (!_visitedDirs.Add(ResolveRealPath(dirItem.FullPath)))
                                {
                                    // Already-visited target: a link loop (or hardlinked dir tree). Skip safely.
                                    _onError?.Invoke(dirItem.FullPath, new IOException("Link target already visited (possible loop), skipped."));
                                    continue;
                                }
                            }
                        }

                        dirEntry.DirectoryEntries.Add(dirItem);
                        children.Add(dirItem);
                    }
                    else
                    {
                        dirEntry.FileEntries.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!_options.IgnoreError)
                {
                    throw;
                }

                _onError?.Invoke(dirEntry.FullPath, ex);
            }

            return children;
        }

        private bool IsIncluded(TurFileSystem entry)
        {
            if (_matcher != null)
            {
                // RelativePath is already computed during enumeration; passing it directly
                // avoids the internal GetRelativePath recomputation of the two-arg overload.
                if (!_matcher.Match(entry.RelativePath).HasMatches)
                {
                    return false;
                }
            }

            if (!entry.IsDirectory)
            {
                if (_options.MinFileSize.HasValue && entry.Length < _options.MinFileSize.Value)
                {
                    return false;
                }

                if (_options.MaxFileSize.HasValue && entry.Length > _options.MaxFileSize.Value)
                {
                    return false;
                }

                if (_options.CreateAfter.HasValue && entry.CreationTime <= _options.CreateAfter.Value)
                {
                    return false;
                }

                if (_options.CreateBefore.HasValue && entry.CreationTime >= _options.CreateBefore.Value)
                {
                    return false;
                }

                if (_options.LastModifyAfter.HasValue && entry.LastModifyTime <= _options.LastModifyAfter.Value)
                {
                    return false;
                }

                if (_options.LastModifyBefore.HasValue && entry.LastModifyTime >= _options.LastModifyBefore.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private class TurFileEntry
        {
            public string Name { get; set; }

            public string FullPath { get; set; }

            public string RelativePath { get; set; }

            public long Length { get; set; }

            public DateTime? CreationTime { get; set; }

            public DateTime? LastModifyTime { get; set; }
        }

        private class TurDirectoryEntry : TurFileEntry
        {
            public List<TurFileEntry> FileEntries { get; } = new List<TurFileEntry>();

            public List<TurDirectoryEntry> DirectoryEntries { get; } = new List<TurDirectoryEntry>();
        }
    }
}
