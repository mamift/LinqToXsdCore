using System.IO;

namespace Xml.Schema.Linq.Tests
{
    public static class Extensions
    {
        /// <summary>
        /// Copies an existing <paramref name="dir"/> to a <paramref name="destination"/> directory, that may or may not exist.
        /// <para>https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories</para>
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="destination"></param>
        /// <param name="copySubDirs"></param>
        /// <param name="overwrite">Set to <c>true</c> to always overwrite. Defaults to <c>false</c>.</param>
        /// <returns>Returns a <see cref="DirectoryInfo"/> to the destination.</returns>
        public static DirectoryInfo Copy(this DirectoryInfo dir, string destination, bool copySubDirs = true, bool overwrite = false)
        {
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: '{dir.FullName}'");

            var dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            var theNewDir = !Directory.Exists(destination)
                ? Directory.CreateDirectory(destination)
                : new DirectoryInfo(destination);

            // Get the files in the directory and copy them to the new location.
            foreach (var file in dir.GetFiles()) {
                var tempPath = Path.Combine(destination, file.Name);
                file.CopyTo(tempPath, overwrite);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (!copySubDirs) return theNewDir;
            foreach (var subDir in dirs) {
                var tempPath = Path.Combine(destination, subDir.Name);
                var _ = subDir.Copy(tempPath, copySubDirs, overwrite);
            }

            return theNewDir;
        }

        /// <summary>
        /// Deletes files inside the current <paramref name="dir"/>, filtering by a <paramref name="filter"/>.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="filter"></param>
        public static void DeleteFilesInside(this DirectoryInfo dir, string filter)
        {
            var files = dir.GetFiles(filter, SearchOption.AllDirectories);
            foreach (var file in files) {
                File.Delete(file.FullName);
            }
        }
    }
}