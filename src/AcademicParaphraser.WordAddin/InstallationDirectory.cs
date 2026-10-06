using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace AcademicParaphraser.WordAddin
{
    internal static class InstallationDirectory
    {
        public static string Resolve(Assembly assembly)
        {
            var origin = new Uri(assembly.CodeBase);
            string? directory = origin.IsFile ? Path.GetDirectoryName(origin.LocalPath) : null;
            if (IsInstallation(directory))
                return directory!;
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Office\Word\Addins\AcademicParaphraser.WordAddin"))
            {
                string? value = key?.GetValue("Manifest") as string;
                if (value != null)
                {
                    const string suffix = "|vstolocal";
                    if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                        value = value.Substring(0, value.Length - suffix.Length);
                    if (Uri.TryCreate(value, UriKind.Absolute, out var manifest) && manifest.IsFile
                        && string.Equals(Path.GetFileName(manifest.LocalPath), "AcademicParaphraser.WordAddin.vsto", StringComparison.OrdinalIgnoreCase))
                    {
                        directory = Path.GetDirectoryName(manifest.LocalPath);
                        if (IsInstallation(directory))
                            return directory!;
                    }
                }
            }
            directory = Path.GetDirectoryName(assembly.Location);
            if (IsInstallation(directory))
                return directory!;
            throw new DirectoryNotFoundException("Akademik Parafraz kurulum dizini bulunamadı.");
        }

        private static bool IsInstallation(string? directory) => directory != null
            && File.Exists(Path.Combine(directory, "AcademicParaphraser.WordAddin.vsto"))
            && File.Exists(Path.Combine(directory, "AcademicParaphraser.WordAddin.dll"))
            && File.Exists(Path.Combine(directory, "runtime", "java", "bin", "java.exe"))
            && File.Exists(Path.Combine(directory, "nlp", "turkish-nlp-1.0.0.jar"));
    }
}
