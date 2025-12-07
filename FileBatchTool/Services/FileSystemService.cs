using System;
using System.Collections.Generic;
using System.IO;
using FileBatchTool.Core;

namespace FileBatchTool.Services;


public interface IFileSystemService
{
    void ExecuteOperations(IEnumerable<FileOperation> operations);
    void CreateTextFile(string directory, string fileName, string content);

    IReadOnlyList<string> GetFilesByExtension(string directory, string extension, bool recursive);
    int DeleteFiles(IEnumerable<string> filePaths);

    string ReadAllText(string path);
    void WriteAllText(string path, string content);
}


public sealed class FileSystemService : IFileSystemService
{
    public string ReadAllText(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path must not be empty.", nameof(path));

        return File.ReadAllText(path);
    }

    public void WriteAllText(string path, string content)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path must not be empty.", nameof(path));

        File.WriteAllText(path, content ?? string.Empty);
    }

    public void ExecuteOperations(IEnumerable<FileOperation> operations)
    {
        ArgumentNullException.ThrowIfNull(operations);

        foreach (var op in operations)
        {
            switch (op.Mode)
            {
                case FileOperationMode.Copy:
                    File.Copy(op.SourcePath, op.TargetPath, overwrite: false);
                    break;

                case FileOperationMode.Rename:
                    if (!string.Equals(op.SourcePath, op.TargetPath, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Move(op.SourcePath, op.TargetPath, overwrite: false);
                    }
                    break;
            }
        }
    }

    public void CreateTextFile(string directory, string fileName, string content)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Directory must not be empty.", nameof(directory));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name must not be empty.", nameof(fileName));

        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, content ?? string.Empty);
    }

    public IReadOnlyList<string> GetFilesByExtension(string directory, string extension, bool recursive)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Directory must not be empty.", nameof(directory));

        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"Directory not found: {directory}");

        var ext = FileNaming.NormalizeExtension(extension);

        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        var files = Directory.EnumerateFiles(directory, "*", option);
        var result = new List<string>();

        foreach (var path in files)
        {
            if (string.Equals(Path.GetExtension(path), ext, StringComparison.OrdinalIgnoreCase))
                result.Add(path);
        }

        return result;
    }

    public int DeleteFiles(IEnumerable<string> filePaths)
    {
        if (filePaths is null) throw new ArgumentNullException(nameof(filePaths));

        int deleted = 0;

        foreach (var path in filePaths)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    deleted++;
                }
            }
            catch
            {
                // Молча пропускаем неудачные удаления, счётчик не увеличиваем
            }
        }

        return deleted;
    }
}
