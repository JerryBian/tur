using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Tur.Util
{
    public static class FileUtil
    {
        private const int PartialHashBytes = 4096;

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
                // Rent instead of allocating: buffers are up to 3MB each and this runs
                // per candidate pair, which otherwise floods the large object heap.
                var first = ArrayPool<byte>.Shared.Rent(maxBytesScan);
                var second = ArrayPool<byte>.Shared.Rent(maxBytesScan);
                try
                {
                    for (var i = 0; i < iterations; i++)
                    {
                        var firstBytes = await f1.ReadAsync(first.AsMemory(0, maxBytesScan), CancellationToken.None);
                        var secondBytes = await f2.ReadAsync(second.AsMemory(0, maxBytesScan), CancellationToken.None);
                        if (firstBytes != secondBytes)
                        {
                            return false;
                        }

                        if (!AreBytesEqual(first.AsSpan(0, maxBytesScan), second.AsSpan(0, maxBytesScan)))
                        {
                            return false;
                        }
                    }

                    return true;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(first);
                    ArrayPool<byte>.Shared.Return(second);
                }
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

        /// <summary>
        /// Computes the SHA256 hex hash of a file. When partial is true only the first
        /// 4KB are hashed (cheap pre-filter). Returns null for unreadable files when
        /// ignoreError is set; otherwise rethrows.
        /// </summary>
        public static async Task<string> GetHashAsync(string file, bool partial, bool ignoreError, CancellationToken cancellationToken)
        {
            try
            {
                await using var stream = File.OpenRead(file);
                if (partial)
                {
                    var length = (int)Math.Min(PartialHashBytes, stream.Length);
                    var buffer = ArrayPool<byte>.Shared.Rent(length);
                    try
                    {
                        var read = 0;
                        while (read < length)
                        {
                            var n = await stream.ReadAsync(buffer.AsMemory(read, length - read), cancellationToken);
                            if (n == 0)
                            {
                                break;
                            }

                            read += n;
                        }

                        return Convert.ToHexString(SHA256.HashData(buffer.AsSpan(0, read)));
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }

                var hash = await SHA256.HashDataAsync(stream, cancellationToken);
                return Convert.ToHexString(hash);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                if (!ignoreError)
                {
                    throw;
                }

                return null;
            }
        }

        private static bool AreBytesEqual(ReadOnlySpan<byte> b1, ReadOnlySpan<byte> b2)
        {
            return b1.SequenceEqual(b2);
        }
    }
}
