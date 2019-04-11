using System;
using System.IO;
using System.Linq;
using LinqToXsd;

// ReSharper disable once CheckNamespace
namespace Xml.Schema.Linq
{
    public partial class Configuration
    {
        /// <summary>
        /// Saves the current instance
        /// </summary>
        /// <param name="fileNameOrFullPath"></param>
        public void SaveNoOverwrite(string fileNameOrFullPath)
        {
            if (fileNameOrFullPath == null) throw new ArgumentNullException(nameof(fileNameOrFullPath));

            if (!Path.IsPathRooted(fileNameOrFullPath))
                fileNameOrFullPath = Path.GetFullPath(fileNameOrFullPath);

            var fileNameComponent = Path.GetFileName(fileNameOrFullPath);
            var initialFileName = fileNameComponent; // save for referencing when we find and replace at the end
            var initialDir = Path.GetDirectoryName(fileNameOrFullPath);

            while (File.Exists(Path.Combine(initialDir, fileNameComponent))) // yes this needs to be refresh using Path.Combine each iteration
            {
                var splitFileName = fileNameComponent.Split('.');
                var firstHalf = splitFileName.First();
                firstHalf = firstHalf.AppendNumberToString();
                fileNameComponent = string.Join(".", firstHalf, splitFileName.Last());
            }

            var @out = fileNameOrFullPath.Replace(initialFileName, fileNameComponent);
            Save(@out);
        }
    }
}