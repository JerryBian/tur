﻿using Microsoft.Extensions.FileSystemGlobbing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tur.Model;

namespace Tur.Util
{
    public static class FileUtil
    {
        private static readonly int _maxBytesScan;

        static FileUtil()
        {
            var gcMemoryInfo = GC.GetGCMemoryInfo();
            _maxBytesScan = Convert.ToInt32(Math.Min(gcMemoryInfo.TotalAvailableMemoryBytes / 10, 3 * 1024 * 1024));
        }

        public static async Task<bool> IsSameFileAsync(string file1, string file2, bool ignoreError)
        {
            try
            {
                if (string.Equals(file1, file2))
                {
                    return true;
                }

                FileInfo fileInfo1 = new(file1);
                FileInfo fileInfo2 = new(file2);
                if (fileInfo1.Length != fileInfo2.Length)
                {
                    return false;
                }

                var maxBytesScan = Convert.ToInt32(Math.Min(_maxBytesScan, fileInfo1.Length));
                var iterations = (int)Math.Ceiling((double)fileInfo1.Length / maxBytesScan);
                await using var f1 = fileInfo1.OpenRead();
                await using var f2 = fileInfo2.OpenRead();
                var first = new byte[maxBytesScan];
                var second = new byte[maxBytesScan];

                for (var i = 0; i < iterations; i++)
                {
                    var firstBytes = await f1.ReadAsync(first.AsMemory(0, maxBytesScan), CancellationToken.None);
                    var secondBytes = await f2.ReadAsync(second.AsMemory(0, maxBytesScan), CancellationToken.None);
                    if (firstBytes != secondBytes)
                    {
                        return false;
                    }

                    if (!AreBytesEqual(first, second))
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                if (!ignoreError)
                {
                    throw;
                }

                return false;
            }
        }

        private static bool AreBytesEqual(ReadOnlySpan<byte> b1, ReadOnlySpan<byte> b2)
        {
            return b1.SequenceEqual(b2);
        }

        public static IEnumerable<FileSystemItem> EnumerateFiles(
            string dir,
            List<string> includes = null,
            List<string> excludes = null,
            DateTime createdBefore = default,
            DateTime createdAfter = default,
            DateTime lastModifiedBefore = default,
            DateTime lastModifiedAfter = default)
        {
            if (!Directory.Exists(dir))
            {
                yield break;
            }

            Matcher m = new();
            if (includes == null || !includes.Any())
            {
                _ = m.AddInclude("**");
            }
            else
            {
                includes.ForEach(x => m.AddInclude(x));
            }

            excludes?.ForEach(x => m.AddExclude(x));

            foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            {
                FileSystemItem entry = new(false)
                {
                    FullPath = file
                };
                if (!m.Match(dir, file).HasMatches)
                {
                    continue;
                }

                var flag = true;
                if (createdBefore != default || createdAfter != default)
                {
                    var fileCreationTime = File.GetCreationTime(file);
                    var min = createdAfter == default ? DateTime.MinValue : createdAfter;
                    var max = createdBefore == default ? DateTime.MaxValue : createdBefore;
                    flag = fileCreationTime >= min && fileCreationTime <= max;
                }

                if (flag && (lastModifiedAfter != default || lastModifiedBefore != default))
                {
                    var fileLastModifyTime = File.GetLastWriteTime(file);
                    var min = lastModifiedAfter == default ? DateTime.MinValue : lastModifiedAfter;
                    var max = lastModifiedBefore == default ? DateTime.MaxValue : lastModifiedBefore;
                    flag = fileLastModifyTime >= min && fileLastModifyTime <= max;
                }

                if (!flag)
                {
                    continue;
                }

                yield return entry;
            }
        }

        public static IEnumerable<FileSystemItem> EnumerateDirectories(
            string dir,
            List<string> includes = null,
            List<string> excludes = null)
        {
            if (!Directory.Exists(dir))
            {
                yield break;
            }

            Matcher m = new();
            if (includes == null || !includes.Any())
            {
                _ = m.AddInclude("**");
            }
            else
            {
                includes.ForEach(x => m.AddInclude(x));
            }

            excludes?.ForEach(x => m.AddExclude(x));

            foreach (var item in Directory.EnumerateDirectories(dir, "*", SearchOption.AllDirectories))
            {
                FileSystemItem entry = new(true)
                {
                    FullPath = item
                };
                if (!m.Match(dir, item).HasMatches)
                {
                    continue;
                }

                yield return entry;
            }
        }
    }
}
