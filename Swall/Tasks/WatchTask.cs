using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;
using Swall.Runner;

namespace Swall.Tasks
{
    internal sealed class WatchTask : SwallTask
    {
        private readonly string root;
        private readonly IReadOnlyDictionary<string, (string[] Patterns, string[] On)> watches;

        private readonly TaskRunner taskRunner;
        private readonly CancellationToken waitToken;

        private DirectoryInfo rootDirectory;
        private Dictionary<string, (Matcher Matcher, WatcherChangeTypes[] WatcherChangeTypes)> watchMatchers;

        public override string Name => "watch";

        public WatchTask(IReadOnlyDictionary<string, object> config, TaskRunner taskRunner, CancellationToken waitToken) : base(config)
        {
            root = Config["root"]?.ToString();

            watches = (Config["watches"] as object[])?
                            .Select(o => o as Dictionary<string, object>)
                            .ToDictionary(
                                k => k["task"]?.ToString(),
                                v => (
                                    (v["patterns"] as object[])?.Select(p => p.ToString()).ToArray(),
                                    ((v.TryGetValue("on", out var on) ? on : null) as object[])?.Select(p => p.ToString()).ToArray()
                                )
                            );

            this.taskRunner = taskRunner;
            this.waitToken = waitToken;
        }

        /// <summary>
        /// Watches for any file changes in the specified directories and queue any tasks associated with the changed files.
        /// </summary>
        /// <param name="subTask"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        protected override async Task Execute(string subTask = null, string parameters = null)
        {
            watchMatchers = new Dictionary<string, (Matcher Matcher, WatcherChangeTypes[] WatcherChangeTypes)>();

            foreach (var watch in watches)
            {
                var matcher = new Matcher();

                foreach (var pattern in watch.Value.Patterns)
                {
                    if (pattern.StartsWith('!'))
                    {
                        matcher.AddExclude(pattern.TrimStart('!'));
                    }
                    else
                    {
                        matcher.AddInclude(pattern);
                    }
                }

                var watcherChangeTypes = new List<WatcherChangeTypes>();

                if (watch.Value.On?.Length > 0)
                {
                    foreach (var on in watch.Value.On)
                    {
                        switch (on)
                        {
                            case "change":
                                watcherChangeTypes.Add(WatcherChangeTypes.Changed);
                                watcherChangeTypes.Add(WatcherChangeTypes.Renamed);
                                break;
                            case "create":
                                watcherChangeTypes.Add(WatcherChangeTypes.Created);
                                break;
                            case "delete":
                                watcherChangeTypes.Add(WatcherChangeTypes.Deleted);
                                break;
                        }
                    }
                }
                else
                {
                    // include all change types
                    watcherChangeTypes.Add(WatcherChangeTypes.Changed);
                    watcherChangeTypes.Add(WatcherChangeTypes.Created);
                    watcherChangeTypes.Add(WatcherChangeTypes.Deleted);
                    watcherChangeTypes.Add(WatcherChangeTypes.Renamed);
                }

                watchMatchers.Add(watch.Key, (matcher, watcherChangeTypes.ToArray()));
            }

            rootDirectory = new DirectoryInfo(root);

            using var fileSystemWatcher = new FileSystemWatcher()
            {
                Path = rootDirectory.FullName,
                IncludeSubdirectories = true,
                Filter = "*.*",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
            };

            fileSystemWatcher.Changed += File_Changed;
            fileSystemWatcher.Created += File_Changed;
            fileSystemWatcher.Deleted += File_Changed;
            fileSystemWatcher.Renamed += File_Changed;

            fileSystemWatcher.EnableRaisingEvents = true;

            WriteToConsole($"Started in {rootDirectory.FullName}");

            while (!waitToken.IsCancellationRequested)
            {
                await Task.Delay(500);
            }

            WriteToConsole("Stopping...");

            fileSystemWatcher.EnableRaisingEvents = false;
        }

        private void File_Changed(object sender, FileSystemEventArgs e)
        {
            var filePath = Path.GetRelativePath(rootDirectory.FullName, e.FullPath)
                                    .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            foreach (var watchMatcher in watchMatchers)
            {
                if (watchMatcher.Value.Matcher.Match(filePath).HasMatches)
                {
                    if (!watchMatcher.Value.WatcherChangeTypes.Contains(e.ChangeType))
                    {
                        // don't run for current change type
                        continue;
                    }

                    WriteToConsole($"File changed: {e.FullPath}");

                    var taskNames = watchMatcher.Key.Split('|');
                    var taskName = taskNames[0];
                    var subTask = (taskNames.Length > 1 ? taskNames[1] : null);

                    taskRunner.AddTask(taskName, subTask, e.FullPath);
                }
            }
        }
    }
}
