using System;
using System.Threading;
using System.Threading.Tasks;
using Swall.Configuration;
using Swall.Runner;

[assembly: CLSCompliant(true)]

namespace Swall
{
    internal sealed class Program
    {
        private static async Task Main(string[] args)
        {
            try
            {
                using var waitTokenSource = new CancellationTokenSource();

                Console.CancelKeyPress += (sender, e) =>
                {
                    waitTokenSource.Cancel();
                    e.Cancel = true;
                };

                var taskName = args.Length > 0 ? args[0] : "default";

                var taskRunner = new TaskRunner(waitTokenSource.Token);

                taskRunner.AddTask(taskName);

                await taskRunner.Start();
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                ConsoleWriter.WriteLine($"Error - {ex.Message}", ConsoleColor.Red);
                ConsoleWriter.WriteLine(ex.StackTrace, ConsoleColor.Red, false);

                if (ex.InnerException != null)
                {
                    ConsoleWriter.WriteLine(ex.InnerException.Message, ConsoleColor.Red, false);
                    ConsoleWriter.WriteLine(ex.InnerException.StackTrace, ConsoleColor.Red, false);
                }
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
        }
    }
}
