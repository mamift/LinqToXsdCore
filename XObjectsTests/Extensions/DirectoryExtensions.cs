#nullable enable
using System;
using System.IO;
using System.Linq;

namespace Xml.Schema.Linq.Tests.Extensions;

public static class DirectoryExtensions
{
    /// <summary>
    /// Traverses upward from the starting directory until a directory whose name matches ancestorFolderName is found.
    /// Returns a new DirectoryInfo for that ancestor. Throws if not found.
    /// </summary>
    /// <example>
    /// var start = new DirectoryInfo(@"C:\Projects\GitHub\LinqToXsdCore\GeneratedSchemaLibraries\Atom\bin\Debug\netstandard2.0");
    /// var ancestor = DirectoryUtilities.AscendToFolder(start, "GeneratedSchemaLibraries");
    /// </example>
    /// <param name="startingDirectory">The directory to start from.</param>
    /// <param name="ancestorFolderName">The name of the ancestor folder to locate (case-insensitive on Windows).</param>
    /// <returns>DirectoryInfo of the ancestor folder.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="DirectoryNotFoundException"/>
    public static DirectoryInfo AscendToFolder(this DirectoryInfo startingDirectory, string ancestorFolderName)
    {
        if (startingDirectory == null) throw new ArgumentNullException(nameof(startingDirectory));
        if (string.IsNullOrWhiteSpace(ancestorFolderName)) throw new ArgumentException("Folder name must be supplied.", nameof(ancestorFolderName));

        var current = startingDirectory;
        while (current != null) {
            if (string.Equals(current.Name, ancestorFolderName, StringComparison.OrdinalIgnoreCase)) {
                // Return a fresh instance (optional; current itself would also be fine).
                return new DirectoryInfo(current.FullName);
            }
            var possibleExistingAtCurrentLevelAsSubdirs = current.EnumerateDirectories(ancestorFolderName).ToList();
            if (possibleExistingAtCurrentLevelAsSubdirs.Any()) {
                return possibleExistingAtCurrentLevelAsSubdirs.Single(e => e.Name == ancestorFolderName);
            }
            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Ancestor folder '{ancestorFolderName}' was not found starting from '{startingDirectory.FullName}'.");
    }

    public static DirectoryInfo AscendToFolder(this string startingDirPath, string ancestorFolderName)
    {
        if (string.IsNullOrEmpty(startingDirPath)) throw new ArgumentNullException(nameof(startingDirPath));
        
        var startingDirectory = new DirectoryInfo(startingDirPath);
        return startingDirectory.AscendToFolder(ancestorFolderName);
    }
    
    /// <summary>
    /// Traverses downward (depth-first) from startingDirectory looking for a descendant folder
    /// whose name matches descendantFolderName (case-insensitive). Returns a new DirectoryInfo if found.
    /// Throws InvalidOperationException if there are no subdirectories to search. Throws DirectoryNotFoundException if not found.
    /// </summary>
    /// <example>
    /// var start = new DirectoryInfo(@"C:\Projects\GitHub\LinqToXsdCore");
    /// var descendant = DirectoryExtensions.DescendToFolder(start, "GeneratedSchemaLibraries");
    /// </example>
    /// <param name="startingDirectory">Root directory to begin the descent.</param>
    /// <param name="descendantFolderName">Target descendant directory name.</param>
    /// <returns>DirectoryInfo of the matching descendant.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="InvalidOperationException">Thrown when startingDirectory has no subdirectories.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the descendant folder is not found.</exception>
    public static DirectoryInfo DescendToFolder(this DirectoryInfo startingDirectory, string descendantFolderName)
    {
        if (startingDirectory == null) throw new ArgumentNullException(nameof(startingDirectory));
        if (string.IsNullOrWhiteSpace(descendantFolderName)) throw new ArgumentException("Folder name must be supplied.", nameof(descendantFolderName));
        if (!startingDirectory.Exists) throw new DirectoryNotFoundException($"Starting directory does not exist: '{startingDirectory.FullName}'.");

        // Quick check for absence of any subdirectories.
        using (var enumerator = startingDirectory.EnumerateDirectories().GetEnumerator()) {
            if (!enumerator.MoveNext()) {
                throw new InvalidOperationException($"Directory '{startingDirectory.FullName}' has no subdirectories to descend into.");
            }
        }

        DirectoryInfo? result = DepthFirst(startingDirectory, descendantFolderName);
        if (result == null) {
            throw new DirectoryNotFoundException(
                $"Descendant folder '{descendantFolderName}' was not found below '{startingDirectory.FullName}'.");
        }

        return result;

        static DirectoryInfo? DepthFirst(DirectoryInfo current, string targetName)
        {
            foreach (var dir in SafeEnumerateDirectories(current)) {
                if (string.Equals(dir.Name, targetName, StringComparison.OrdinalIgnoreCase)) {
                    return new DirectoryInfo(dir.FullName);
                }

                var deeper = DepthFirst(dir, targetName);
                if (deeper != null) return deeper;
            }
            return null;
        }

        static DirectoryInfo[] SafeEnumerateDirectories(DirectoryInfo directory)
        {
            try {
                return directory.GetDirectories();
            } catch (UnauthorizedAccessException) {
                // Skip directories we cannot access.
                return Array.Empty<DirectoryInfo>();
            } catch (DirectoryNotFoundException) {
                return Array.Empty<DirectoryInfo>();
            }
        }
    }

    public static FileInfo FindFileRecursively(this DirectoryInfo dir, string fileName)
    {
        return dir.GetFiles(fileName, SearchOption.AllDirectories).Single();
    }
}
