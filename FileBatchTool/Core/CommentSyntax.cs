using System;

namespace FileBatchTool.Core;

public sealed class CommentSyntax
{
    public string? SingleLine { get; }
    public string? MultiLineStart { get; }
    public string? MultiLineEnd { get; }

    public CommentSyntax(string? singleLine, string? multiLineStart, string? multiLineEnd)
    {
        SingleLine = string.IsNullOrWhiteSpace(singleLine) ? null : singleLine;
        MultiLineStart = string.IsNullOrWhiteSpace(multiLineStart) ? null : multiLineStart;
        MultiLineEnd = string.IsNullOrWhiteSpace(multiLineEnd) ? null : multiLineEnd;
    }

    public static CommentSyntax? ForExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return null;

        var ext = FileNaming.NormalizeExtension(extension).ToLowerInvariant();

        return ext switch
        {
            ".c"
                or ".h"
                or ".cpp"
                or ".hpp"
                or ".cc"
                or ".cxx"
                or ".cs"
                or ".java"
                or ".js"
                or ".ts"
                    => new CommentSyntax("//", "/*", "*/"),
            ".m"    => new CommentSyntax("%", "%{", "%}"),
            ".py"   => new CommentSyntax("#", null, null),
            ".sh"
                or ".bash"
                    => new CommentSyntax("#", null, null),
            _ => null,
        };
    }
}
