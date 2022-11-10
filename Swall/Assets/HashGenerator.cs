using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Swall.IO;

namespace Swall.Assets
{
    internal sealed class HashGenerator
    {
        private static readonly UTF8Encoding UTF8WithoutBOM = new UTF8Encoding(false);

        /// <summary>
        /// Placeholder used during the file content generation and to be replaced with the generated hash afterwards.
        /// </summary>
        public const string Placeholder = "{revision}";

        /// <summary>
        /// Generate revision hash from the file content.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string GenerateRevisionHash(string content)
        {
            var contentBytes = UTF8WithoutBOM.GetBytes(content);

            var hash = SHA256.HashData(contentBytes);

            var hexHash = BitConverter.ToString(hash);

            var revisionHash = string.Join(string.Empty, hexHash.Split('-').Take(5)).ToLower();

            return revisionHash;
        }

        /// <summary>
        /// Generate integrity hash from the file content.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static async Task<string> GenerateIntegrityHash(string filePath)
        {
            var revisionFile = new FileInfo(filePath);

            var hashBytes = SHA384.HashData(await FileAccessor.ReadAllBytes(revisionFile.FullName));

            var hash = $"sha384-{Convert.ToBase64String(hashBytes)}";

            return hash;
        }
    }
}
