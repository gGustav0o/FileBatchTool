using System;
using System.Collections.Generic;
using System.IO;
using FileBatchTool.Core;

namespace FileBatchTool.Services;

public interface IFileSystemService
{
    void ExecuteOperations(IEnumerable<FileOperation> operations);
    void CreateTextFile(string directory, string fileName, string content);
}

public sealed class FileSystemService : IFileSystemService
{
    public void ExecuteOperations(IEnumerable<FileOperation> operations) {
        ArgumentNullException.ThrowIfNull(operations);

        foreach (var op in operations) {
            switch (op.Mode) {
                case FileOperationMode.Copy:
                    File.Copy(op.SourcePath, op.TargetPath, overwrite: false);
                    break;

                case FileOperationMode.Rename:
                    if (!string.Equals(op.SourcePath, op.TargetPath, StringComparison.OrdinalIgnoreCase)) {
                        File.Move(op.SourcePath, op.TargetPath, overwrite: false);
                    }
                    break;
            }
        }
    }

    public void CreateTextFile(string directory, string fileName, string content) {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Directory must not be empty.", nameof(directory));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name must not be empty.", nameof(fileName));

        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, content ?? string.Empty);
    }
}
