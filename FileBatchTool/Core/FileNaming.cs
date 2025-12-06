using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileBatchTool.Core;

public static class FileNaming
{
    public static string ChangeExtension(string path, string newExtension) {
        ArgumentNullException.ThrowIfNull(path);
        if (string.IsNullOrWhiteSpace(newExtension))
            throw new ArgumentException("New extension must not be empty.", nameof(newExtension));

        var ext = newExtension.StartsWith('.') ? newExtension : "." + newExtension;
        return Path.ChangeExtension(path, ext);
    }

    public static IReadOnlyList<FileOperation> BuildOperations(
        IEnumerable<string> sourceFiles
        , string newExtension
        , FileOperationMode mode
    ) {
        ArgumentNullException.ThrowIfNull(sourceFiles);

        return sourceFiles
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(src => new FileOperation(
                sourcePath: src,
                targetPath: ChangeExtension(src, newExtension),
                mode: mode))
            .ToList();
    }

    public static IReadOnlyList<string> GetResultPaths(IEnumerable<FileOperation> operations)
    {
        ArgumentNullException.ThrowIfNull(operations);

        return operations
            .Select(o => o.TargetPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

}
