using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Swall.IO;

namespace Swall.Tasks
{
    internal sealed class AssetsTask : SwallTask
    {
        private readonly IReadOnlyDictionary<string, string> paths;

        public override string Name => "assets";

        public AssetsTask(IReadOnlyDictionary<string, object> config) : base(config)
        {
            paths = Config.ToDictionary(k => k.Key, v => v.Value?.ToString());
        }

        /// <summary>
        /// Copies matched asset files from source directory to destination directory.
        /// </summary>
        /// <param name="subTask"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        protected override async Task Execute(string subTask = null, string parameters = null)
        {
            foreach (var path in paths)
            {
                var src = path.Key;
                var dest = path.Value;

                var destDirectory = new DirectoryInfo(dest);

                if (!destDirectory.Exists)
                {
                    Directory.CreateDirectory(destDirectory.FullName);
                }

                var srcDir = string.Empty;
                var srcGlob = string.Empty;

                var srcParts = src.Split("/**");

                if (srcParts.Length > 1)
                {
                    srcDir = srcParts[0];
                    srcGlob = $"**{srcParts[1]}";
                }
                else
                {
                    srcDir = Directory.GetCurrentDirectory();
                    srcGlob = src;
                }

                var srcMatcher = new Matcher();
                srcMatcher.AddInclude(srcGlob);

                var srcDirectory = new DirectoryInfo(srcDir);

                var srcMatchResult = srcMatcher.Execute(new DirectoryInfoWrapper(srcDirectory));

                foreach (var stem in srcMatchResult.Files.Select(m => m.Stem))
                {
                    WriteToConsole($"Copying {stem}");

                    var srcPath = Path.GetFullPath(stem, srcDirectory.FullName);
                    var destPath = Path.GetFullPath(stem, destDirectory.FullName);

                    var destDirectoryPath = Path.GetDirectoryName(destPath);

                    if (!Directory.Exists(destDirectoryPath))
                    {
                        Directory.CreateDirectory(destDirectoryPath);
                    }

                    await FileAccessor.Copy(srcPath, destPath, true);
                }
            }

            await Task.CompletedTask;
        }
    }
}
