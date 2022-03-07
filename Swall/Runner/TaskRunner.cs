using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Swall.Configuration;
using Swall.IO;
using Swall.Tasks;
using Swall.Yaml;

namespace Swall.Runner
{
    internal class TaskRunner
    {
        private readonly IReadOnlyDictionary<string, SwallTask> availableTasks;

        private readonly ConcurrentQueue<string> taskQueue;

        private const int MaxThreads = 4;

        private readonly CancellationToken waitToken;

        /// <summary>
        /// Creates instance of TaskRunner, including the available tasks and the task queue.
        /// </summary>
        /// <param name="waitToken"></param>
        public TaskRunner(CancellationToken waitToken)
        {
            this.waitToken = waitToken;

            var configuration = GetYamlConfiguration("swall.yaml").Result;

            availableTasks = new List<SwallTask>()
            {
                new DefaultTask(configuration, this),
                new ServerTask(configuration, waitToken),
                new ScssTask(configuration),
                new JsTask(configuration),
                new HtmlTask(configuration),
                new AssetsTask(configuration),
                new SitemapTask(configuration),
                new CleanTask(configuration),
                new WatchTask(configuration, this, waitToken)
            }.ToDictionary(k => k.Name, v => v);

            taskQueue = new ConcurrentQueue<string>();
        }

        /// <summary>
        /// Reads and parses YAML file containing the task configuration.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static async Task<IReadOnlyDictionary<string, object>> GetYamlConfiguration(string path)
        {
            if (!await FileAccessor.Exists(path))
            {
                throw new ArgumentException($"Cannot load {path}", nameof(path));
            }

            var yamlDeserializer = new YamlDeserializer();

            var yaml = await FileAccessor.ReadAllText(path);

            var dictionary = yamlDeserializer.Deserialize(yaml);

            return dictionary;
        }

        /// <summary>
        /// Starts the task runner.
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            var tasks = new List<Task>();

            for (int n = 0; n < MaxThreads; n++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    while (!waitToken.IsCancellationRequested)
                    {
                        if (taskQueue.TryDequeue(out string fullTaskName))
                        {
                            var taskNames = fullTaskName.Split('|');
                            var taskName = taskNames[0];
                            var subTask = (taskNames.Length > 1 ? taskNames[1] : null);
                            var parameters = (taskNames.Length > 2 ? taskNames[2] : null);

                            var task = availableTasks[taskName];

                            await task.Run(subTask, parameters);
                        }
                        else
                        {
                            Thread.Sleep(250);
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);

            ConsoleWriter.WriteLine("Finished all tasks", ConsoleColor.Green);
        }

        /// <summary>
        /// Add a new task to the runner's task queue.
        /// </summary>
        /// <param name="taskName"></param>
        /// <param name="subTask"></param>
        /// <param name="parameters"></param>
        public void AddTask(string taskName, string subTask = null, string parameters = null)
        {
            var fullTaskName = taskName;

            if (subTask != null)
            {
                fullTaskName += $"|{subTask}";
            }

            if (parameters != null)
            {
                if (subTask == null)
                {
                    // add empty subtask
                    fullTaskName += "|";
                }

                fullTaskName += $"|{parameters}";
            }

            lock (taskQueue)
            {
                if (availableTasks.ContainsKey(taskName) && !taskQueue.Any(t => t == fullTaskName))
                {
                    taskQueue.Enqueue(fullTaskName);
                }
            }
        }
    }
}
