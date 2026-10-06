using System;
using System.IO;
using System.Linq;
namespace AcademicParaphraser.Infrastructure.Diagnostics
{
    public sealed class SafeLog
    {
        private readonly string folder; private readonly object sync = new object();
        public SafeLog(string folder)
        {
            this.folder = folder;
        }
        public void Write(string eventCode, Exception? exception = null)
        {
            lock (sync)
            {
                try
                {
                    Directory.CreateDirectory(folder);
                    string code = new string(eventCode.Where(c => char.IsLetterOrDigit(c) || c == '_').Take(80).ToArray());
                    string path = Path.Combine(folder, DateTime.UtcNow.ToString("yyyy-MM-dd") + ".log");
                    File.AppendAllText(path, DateTime.UtcNow.ToString("O") + " " + code + (exception == null ? "" : " " + exception.GetType().Name) + Environment.NewLine);
                    foreach (var f in Directory.GetFiles(folder, "*.log").OrderByDescending(File.GetLastWriteTimeUtc).Skip(7))
                        File.Delete(f);
                }
                // A logging failure must never replace the user's original Word error.
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { }
            }
        }
    }
}
