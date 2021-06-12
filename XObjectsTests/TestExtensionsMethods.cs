using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using Xml.Schema.Linq.Extensions;

namespace Xml.Schema.Linq.Tests
{
    /// <summary>
    /// Extension methods used specifically in unit tests.
    /// </summary>
    public static class TestExtensionsMethods
    {
        /// <summary>
        /// Copies an existing <paramref name="dir"/> to a <paramref name="destination"/> directory, that may or may not exist.
        /// <para>https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories</para>
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="destination"></param>
        /// <param name="copySubDirs"></param>
        /// <param name="overwrite">Set to <c>true</c> to always overwrite. Defaults to <c>false</c>.</param>
        /// <param name="fileExtensionFilters">Exclude files with these extensions</param>
        /// <returns>Returns a <see cref="DirectoryInfo"/> to the destination.</returns>
        public static DirectoryInfo Copy(this DirectoryInfo dir, string destination, bool copySubDirs = true, bool overwrite = false,
            string[] fileExtensionFilters = null)
        {
            if (fileExtensionFilters == null) {
                fileExtensionFilters = Array.Empty<string>();
            }

            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: '{dir.FullName}'");

            var dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            var theNewDir = !Directory.Exists(destination)
                ? Directory.CreateDirectory(destination)
                : new DirectoryInfo(destination);

            dir.Refresh();
            
            // Get the files in the directory and copy them to the new location.
            FileInfo[] filesInDir = (from file in dir.GetFiles("*", SearchOption.AllDirectories) 
                                     let filteredMatch = fileExtensionFilters.Any(filter => file.Extension.EndsWith(filter))
                                     where !filteredMatch
                                     select file).ToArray();
            foreach (var file in filesInDir) {
                var tempPath = Path.Combine(destination, file.Name);
                file.CopyTo(tempPath, overwrite);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (!copySubDirs) return theNewDir;
            foreach (var subDir in dirs) {
                var tempPath = Path.Combine(destination, subDir.Name);
                var _ = subDir.Copy(tempPath, copySubDirs: copySubDirs, overwrite: overwrite, 
                    fileExtensionFilters: fileExtensionFilters);
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

        /// <summary>
        /// Returns the first <see cref="NamespaceDeclarationSyntax"/> found in the current <paramref name="tree"/>.
        /// </summary>
        /// <param name="tree"></param>
        /// <returns></returns>
        public static NamespaceDeclarationSyntax GetNamespaceRoot(this SyntaxTree tree)
        {
            var root = tree.GetRoot();
            var namespaceNode = root.DescendantNodes()
                                    .First(dn => dn is NamespaceDeclarationSyntax) as NamespaceDeclarationSyntax;

            return namespaceNode;
        }

        /// <summary>
        /// Returns the <see cref="TextWriter"/> of the current <see cref="CodeTypeDeclaration"/>.
        /// </summary>
        /// <param name="codeObject"></param>
        /// <param name="namespaceName"></param>
        /// <returns></returns>
        public static TextWriter ToTextWriter(this CodeTypeDeclaration codeObject, string namespaceName = null)
        {
            if (namespaceName == null) namespaceName = "DefaultCodeNamespace";

            var ccu = new CodeCompileUnit();

            var codeNamespace = new CodeNamespace(namespaceName);
            codeNamespace.Types.Add(codeObject);
            ccu.Namespaces.Add(codeNamespace);

            return ccu.ToStringWriter();
        }

        /// <summary>
        /// Returns all the current <paramref name="nodes"/> as a fully formatted string.
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public static string ToFullString(this IEnumerable<SyntaxNode> nodes, string delimiter = "")
        {
            var sb = new StringBuilder();

            foreach (var node in nodes) {
                sb.Append(node.ToFullString() + delimiter);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Dumps debug strings to the <see cref="TestContext.WorkDirectory"/>.
        /// </summary>
        /// <param name="tc"></param>
        /// <param name="caller"></param>
        /// <param name="debugStrings"></param>
        public static void DumpDebugOutputToFile(this TestContext tc, [CallerMemberName] string caller = null, params string[] debugStrings)
        {
            if (caller.IsEmpty()) throw new ArgumentNullException(nameof(caller));

            var existingFiles = Directory.GetFiles(tc.WorkDirectory, $"{caller}_debug*.log").Select(p => new FileInfo(p));

            var largestNumber = existingFiles.Select(f => Regex.Replace(f.Name, "[^0-9]+", string.Empty))
                .Select(n => n.ParseInt()).Max();

            for (var i = 0; i < debugStrings.Length; i++) {
                var incrementor = largestNumber ?? i;
                if (largestNumber != null) largestNumber++;
                var str = debugStrings[i];

                var outputPath = Path.Join(tc.WorkDirectory, caller + "_debug" + incrementor + ".log");
                File.WriteAllText(outputPath, str);
            }
        }
    }
}
