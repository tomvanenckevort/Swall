using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Swall.Configuration;

namespace Swall.Tasks
{
    internal abstract class SwallTask : IDisposable
    {
        private readonly SemaphoreSlim taskSemaphore;

        protected readonly IReadOnlyDictionary<string, object> Config;

        /// <summary>
        /// Name of the task used to find configuration settings and write console log messages.
        /// </summary>
        public abstract string Name { get; }

        protected SwallTask(IReadOnlyDictionary<string, object> config)
        {
            taskSemaphore = new SemaphoreSlim(1, 1);

            Config = config[Name] as IReadOnlyDictionary<string, object>;
        }

        /// <summary>
        /// Dispose method.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose the semaphore object.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                taskSemaphore.Dispose();
            }
        }

        /// <summary>
        /// Writes message to the console, using the optionally provided colour.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="colour"></param>
        protected void WriteToConsole(string message, ConsoleColor colour = ConsoleColor.Cyan)
        {
            ConsoleWriter.WriteLine($"{Name} - {message}", colour);
        }

        /// <summary>
        /// Executes the task and writes start and end messages to the console.
        /// </summary>
        /// <param name="subTask"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task Run(string subTask = null, string parameters = null)
        {
            await taskSemaphore.WaitAsync();

            try
            {
                WriteToConsole("Starting...");

                await Execute(subTask, parameters);

                WriteToConsole("Finished");
            }
            finally
            {
                taskSemaphore.Release();
            }
        }

        protected abstract Task Execute(string subTask = null, string parameters = null);
    }
}
