namespace FileBatchTool.Core;

public sealed class FileOperation
{
    public string SourcePath { get; init; }
    public string TargetPath { get; init; }
    public FileOperationMode Mode { get; init; }

    public FileOperation(string sourcePath, string targetPath, FileOperationMode mode)
    {
        SourcePath = sourcePath;
        TargetPath = targetPath;
        Mode = mode;
    }
}
