using Microsoft.Extensions.FileSystemGlobbing;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Threading;

namespace Tur.Core
{
    public class TurSystemBuilder
    {
        private readonly string _rootDir;
        private readonly Matcher _matcher;
        private readonly TurBuildOptions _options;
        private readonly CancellationToken _cancellationToken;

        public TurSystemBuilder(string rootDir, TurBuildOptions options, CancellationToken cancellationToken)
        {
            _rootDir = Path.GetFullPath(rootDir);
            _options = options;
            _matcher = BuildMatcher();
            _cancellationToken = cancellationToken;
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

            Build(entry);
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

        private void Build(TurDirectoryEntry dirEntry)
        {
            var fileSystemEnumerable = new FileSystemEnumerable<TurFileEntry>(
                dirEntry.FullPath,
                (ref FileSystemEntry entry) =>
                {
                    var result = entry.IsDirectory ? new TurDirectoryEntry() : new TurFileEntry();
                    result.FullPath = entry.ToFullPath();
                    result.Name = entry.FileName.ToString();
                    result.RelativePath = Path.GetRelativePath(_rootDir, result.FullPath);
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
                    AttributesToSkip = FileAttributes.System,
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = false,
                    ReturnSpecialDirectories = false
                });

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
                        dirEntry.DirectoryEntries.Add(dirItem);
                        Build(dirItem);
                    }
                    else
                    {
                        dirEntry.FileEntries.Add(item);
                    }
                }
            }
            catch (Exception)
            {
                if (!_options.IgnoreError)
                {
                    throw;
                }
            }
        }

        private bool IsIncluded(TurFileSystem entry)
        {
            if (_matcher != null)
            {
                if (!_matcher.Match(_rootDir, entry.FullPath).HasMatches)
                {
                    return false;
                }
            }

            if (!entry.IsDirectory)
            {
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
