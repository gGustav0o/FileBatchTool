using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace FileBatchTool.Services;

public interface IClipboardService
{
    void SetFiles(IEnumerable<string> filePaths);
}

public sealed class ClipboardService : IClipboardService
{
    public void SetFiles(IEnumerable<string> filePaths)
    {
        if (filePaths is null) throw new ArgumentNullException(nameof(filePaths));

        var list = new StringCollection();

        foreach (var path in filePaths)
        {
            if (!string.IsNullOrWhiteSpace(path))
                list.Add(path);
        }

        if (list.Count == 0)
            return;

        System.Windows.Clipboard.SetFileDropList(list);
    }
}
