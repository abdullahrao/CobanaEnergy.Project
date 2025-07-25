using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Logic
{
    public static class Logger
    {
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LOGS");

        public static void Log(string message, [CallerFilePath] string file = "",
            [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }

                string fileName = $"log_{DateTime.Now:yyyy-MM-dd}.txt";
                string fullPath = Path.Combine(LogDirectory, fileName);

                var logEntry = new StringBuilder();
                logEntry.AppendLine("----- LOG ENTRY -----");
                logEntry.AppendLine($"Time: {DateTime.Now:HH:mm:ss}");
                logEntry.AppendLine($"Message: {message}");
                logEntry.AppendLine($"Location: {Path.GetFileName(file)} -> {member}() [Line {line}]");
                logEntry.AppendLine();

                File.AppendAllText(fullPath, logEntry.ToString());
            }
            catch
            {
                // Suppress exceptions from logging
            }
        }
    }
}
