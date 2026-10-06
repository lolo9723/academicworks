using System;
using System.IO;
using System.Security.Cryptography;

namespace AcademicParaphraser.WordHost
{
    public static class NativeSqliteBootstrap
    {
        private static readonly object Sync = new object();

        public static string GetStorageMode()
        {
            var assembly = typeof(SQLitePCL.raw).Assembly;
            var source = new Uri(assembly.CodeBase);
            return source.IsFile && string.Equals(Path.GetFullPath(source.LocalPath), Path.GetFullPath(assembly.Location), StringComparison.OrdinalIgnoreCase)
                ? "DIRECT" : "SHADOW";
        }

        public static void Prepare(string installationDirectory)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                throw new PlatformNotSupportedException("Windows SQLite başlangıcı gerekiyor.");
            string architecture = Environment.Is64BitProcess ? "win-x64" : "win-x86";
            string source = Path.Combine(installationDirectory, "runtimes", architecture, "native", "e_sqlite3.dll");
            ValidateArchitecture(source);
            // SQLitePCL's net461 loader searches beside raw.Assembly.Location. VSTO can shadow-copy
            // managed DLLs without copying the nested runtimes directory to that same cache folder.
            string directory = Path.GetDirectoryName(typeof(SQLitePCL.raw).Assembly.Location)!;
            string target = Path.Combine(directory, "e_sqlite3.dll");
            lock (Sync)
            {
                if (File.Exists(target) && SameHash(source, target))
                    return;
                string temporary = Path.Combine(directory, "e_sqlite3." + Guid.NewGuid().ToString("N") + ".tmp");
                try
                {
                    File.Copy(source, temporary);
                    if (!SameHash(source, temporary))
                        throw new IOException("SQLite kopyası doğrulanamadı.");
                    if (File.Exists(target))
                        File.Replace(temporary, target, null);
                    else
                        File.Move(temporary, target);
                }
                finally
                {
                    if (File.Exists(temporary))
                        File.Delete(temporary);
                }
            }
        }

        private static bool SameHash(string first, string second)
        {
            using (var hash = SHA256.Create())
            using (var a = File.OpenRead(first))
            using (var b = File.OpenRead(second))
                return Convert.ToBase64String(hash.ComputeHash(a)) == Convert.ToBase64String(hash.ComputeHash(b));
        }

        private static void ValidateArchitecture(string path)
        {
            using (var reader = new BinaryReader(File.OpenRead(path)))
            {
                if (reader.ReadUInt16() != 0x5a4d)
                    throw new BadImageFormatException("SQLite DLL başlığı geçersiz.");
                reader.BaseStream.Position = 0x3c;
                int offset = reader.ReadInt32();
                if (offset < 0 || offset > reader.BaseStream.Length - 6)
                    throw new BadImageFormatException("SQLite PE başlığı geçersiz.");
                reader.BaseStream.Position = offset;
                if (reader.ReadUInt32() != 0x00004550 || reader.ReadUInt16() != (Environment.Is64BitProcess ? 0x8664 : 0x14c))
                    throw new BadImageFormatException("SQLite DLL mimarisi Word işlemiyle uyuşmuyor.");
            }
        }
    }
}
