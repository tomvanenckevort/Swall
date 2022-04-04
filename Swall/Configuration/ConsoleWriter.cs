using System;

namespace Swall.Configuration
{
    internal sealed class ConsoleWriter
    {
        private static readonly object consoleLock = new();

        /// <summary>
        /// Writes the specified message to the console, pre-pended with the current timestamp, and using any specified text colour.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="foregroundColour"></param>
        /// <param name="includeTimestamp"></param>
        public static void WriteLine(string value, ConsoleColor? foregroundColour = null, bool includeTimestamp = true)
        {
            lock (consoleLock)
            {
                if (foregroundColour.HasValue)
                {
                    Console.ForegroundColor = foregroundColour.Value;
                }

                Console.WriteLine($"{(includeTimestamp ? $"{DateTime.Now:HH:mm:ss} - " : null)}{value}");

                if (foregroundColour.HasValue)
                {
                    Console.ResetColor();
                }
            }
        }
    }
}
