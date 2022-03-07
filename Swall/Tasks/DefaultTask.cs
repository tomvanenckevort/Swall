using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Swall.Runner;

namespace Swall.Tasks
{
    internal class DefaultTask : SwallTask
    {
        private readonly string[] defaultTaskNames;

        private readonly TaskRunner taskRunner;

        public override string Name => "default";

        public DefaultTask(IReadOnlyDictionary<string, object> config, TaskRunner taskRunner) : base(config)
        {
            defaultTaskNames = (config[Name] as object[])?
                                    .Select(c => c.ToString())?
                                    .ToArray();

            this.taskRunner = taskRunner;
        }

        /// <summary>
        /// Adds each specified task to the task runner's queue.
        /// </summary>
        /// <param name="subTask"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        protected override async Task Execute(string subTask = null, string parameters = null)
        {
            foreach (var taskName in defaultTaskNames)
            {
                taskRunner.AddTask(taskName);
            }

            await Task.CompletedTask;
        }
    }
}
