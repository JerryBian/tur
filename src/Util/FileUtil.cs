using Microsoft.Extensions.FileSystemGlobbing;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using Tur.Model;

namespace Tur.Util
{
    public static class FileUtil
    {
        public static IEnumerable<FileSystemItem> EnumerateFiles(
            string dir,
            bool ignoreError = false,
            List<string> includes = null,
            List<string> excludes = null,
            DateTime createdBefore = default,
            DateTime createdAfter = default,
            DateTime lastModifiedBefore = default,
            DateTime lastModifiedAfter = default)
        {
            if(!Directory.Exists(dir))
            {
                yield break;
            }

            Matcher m = new();
            if (includes == null || !includes.Any())
            {
                m.AddInclude("**");
            }
            else
            {
                includes.ForEach(x => m.AddInclude(x));
            }

            if (excludes != null)
            {
                excludes.ForEach(x => m.AddExclude(x));
            }

            var files = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories);
            using var e = files.GetEnumerator();
            while (true)
            {
                var entry = new FileSystemItem(false);
                try
                {
                    if (!e.MoveNext())
                    {
                        break;
                    };

                    entry.FullPath = e.Current;
                    if(!m.Match(dir, e.Current).HasMatches)
                    {
                        continue;
                    }

                    var flag = true;
                    if(createdBefore != default || createdAfter != default)
                    {
                        var fileCreationTime = File.GetCreationTime(e.Current);
                        var min = createdAfter == default ? DateTime.MinValue : createdAfter;
                        var max = createdBefore== default ? DateTime.MaxValue : createdBefore;
                        flag = fileCreationTime >= min && fileCreationTime <= max;
                    }

                    if(flag && (lastModifiedAfter != default || lastModifiedBefore != default))
                    {
                        var fileLastModifyTime = File.GetLastWriteTime(e.Current);
                        var min = lastModifiedAfter == default ? DateTime.MinValue : lastModifiedAfter;
                        var max = lastModifiedBefore== default ? DateTime.MaxValue : lastModifiedBefore;
                        flag = fileLastModifyTime >= min && fileLastModifyTime <= max;
                    }

                    if(!flag)
                    {
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    if (!ignoreError)
                    {
                        throw;
                    }

                    entry.HasError = true;
                    entry.Error = ex;
                }

                yield return entry;
            }
        }
    }
}
