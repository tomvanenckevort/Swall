using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Swall.IO
{
    internal static class FileAccessor
    {
        private static readonly Dictionary<string, SemaphoreSlim> semaphores = new();

        private static SemaphoreSlim GetSemaphore(string path)
        {
            if (!semaphores.ContainsKey(path))
            {
                semaphores.Add(path, new SemaphoreSlim(1, 1));
            }

            return semaphores[path];
        }

        private static async Task<T> ExecuteWithSemaphore<T>(string path, Func<Task<T>> function)
        {
            var semaphore = GetSemaphore(path);

            await semaphore.WaitAsync();

            try
            {
                return await function();
            }
            catch (IOException ex)
            {
                if (ex.Message.StartsWith("IO_SharingViolation_File"))
                {
                    // wait and try again
                    await Task.Delay(250);

                    return await function();
                }
                else
                {
                    throw ex;
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(path);
        }

        public static async Task<bool> Exists(string path)
        {
            path = NormalizePath(path);

            return await ExecuteWithSemaphore(path, async () => await Task.Run(() => File.Exists(path)));
        }

        public static async Task<string> ReadAllText(string path)
        {
            path = NormalizePath(path);

            return await ExecuteWithSemaphore(path, async () => await File.ReadAllTextAsync(path, Encoding.UTF8));
        }

        public static async Task<byte[]> ReadAllBytes(string path)
        {
            path = NormalizePath(path);

            return await ExecuteWithSemaphore(path, async () => await File.ReadAllBytesAsync(path));
        }

        public static async Task WriteAllText(string path, string contents)
        {
            path = NormalizePath(path);

            await ExecuteWithSemaphore(path, async () => { await File.WriteAllTextAsync(path, contents, Encoding.UTF8); return default(object); });
        }

        public static async Task Copy(string sourceFileName, string destFileName, bool overwrite)
        {
            sourceFileName = NormalizePath(sourceFileName);
            destFileName = NormalizePath(destFileName);

            await ExecuteWithSemaphore(destFileName, async () => await Task.Run(() => { File.Copy(sourceFileName, destFileName, overwrite); return default(object); }));
        }

        public static async Task Delete(string path)
        {
            path = NormalizePath(path);

            await ExecuteWithSemaphore(path, async () => await Task.Run(() => { File.Delete(path); return default(object); }));
        }
    }
}
