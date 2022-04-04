using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Swall.IO;

namespace Swall.Tasks
{
    internal sealed class CleanTask : SwallTask
    {
        private readonly string src;
        private readonly IReadOnlyDictionary<string, string[]> subTasks;

        public override string Name => "clean";

        public CleanTask(IReadOnlyDictionary<string, object> config) : base(config)
        {
            src = Config["src"]?.ToString();

            subTasks = (Config["subTasks"] as object[])?
                            .Select(o => o as Dictionary<string, object>)
                            .ToDictionary(
                                k => k["task"]?.ToString(),
                                v => (v["patterns"] as object[])?.Select(p => p.ToString()).ToArray()
                            );
        }

        /// <summary>
        /// Deletes any files matching the specified name and/or file extensions in the specified directories.
        /// </summary>
        /// <param name="subTask"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        protected override async Task Execute(string subTask = null, string parameters = null)
        {
            var cleanMatcher = new Matcher();

            foreach (var task in subTasks)
            {
                if (subTask != null && task.Key != subTask)
                {
                    continue;
                }

                foreach (var pattern in task.Value)
                {
                    if (pattern.StartsWith('!'))
                    {
                        cleanMatcher.AddExclude(pattern.TrimStart('!'));
                    }
                    else
                    {
                        cleanMatcher.AddInclude(pattern);
                    }
                }
            }

            var srcDirectory = new DirectoryInfo(src);

            var cleanMatchResult = cleanMatcher.Execute(new DirectoryInfoWrapper(srcDirectory));

            foreach (var match in cleanMatchResult.Files)
            {
                var matchPath = Path.GetFullPath(match.Path, srcDirectory.FullName);

                if (subTask != "js" && subTask != "css" && parameters != null && matchPath != parameters)
                {
                    // only delete files that match the path supplied in the parameters
                    continue;
                }

                WriteToConsole($"Deleting {match.Path}");

                await FileAccessor.Delete(matchPath);
            }

            await Task.CompletedTask;
        }
    }
}
