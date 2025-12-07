using System;
using System.Text;

namespace FileBatchTool.Core;

public static class CommentCleaner
{
    public static string RemoveCommentsExceptFlagged(
        string text
        , CommentSyntax syntax
        , string? keepFlag
    ) {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(syntax);

        var afterMulti = RemoveMultiLineComments(text, syntax, keepFlag);
        var afterSingle = RemoveSingleLineComments(afterMulti, syntax, keepFlag);
        return afterSingle;
    }

    private static string RemoveMultiLineComments(
        string text
        , CommentSyntax syntax
        , string? keepFlag
     ) {
        if (syntax.MultiLineStart is null || syntax.MultiLineEnd is null)
            return text;

        var start = syntax.MultiLineStart;
        var end = syntax.MultiLineEnd;

        var sb = new StringBuilder(text.Length);
        int i = 0;
        int n = text.Length;

        while (i < n)
        {
            if (i <= n - start.Length &&
                string.CompareOrdinal(text, i, start, 0, start.Length) == 0)
            {
                int commentStart = i;
                int searchFrom = i + start.Length;
                int endIndex = text.IndexOf(end, searchFrom, StringComparison.Ordinal);

                int commentEndExclusive;
                if (endIndex < 0)
                {
                    commentEndExclusive = n;
                }
                else
                {
                    commentEndExclusive = endIndex + end.Length;
                }

                var commentSegment = text.Substring(commentStart, commentEndExclusive - commentStart);
                bool keep =
                    !string.IsNullOrEmpty(keepFlag)
                    && commentSegment.Contains(keepFlag, StringComparison.Ordinal);

                if (keep)
                {
                    sb.Append(commentSegment);
                }
                else
                {
                    foreach (var ch in commentSegment)
                    {
                        if (ch == '\r' || ch == '\n')
                            sb.Append(ch);
                    }
                }

                i = commentEndExclusive;
            }
            else
            {
                sb.Append(text[i]);
                i++;
            }
        }

        return sb.ToString();
    }

    private static string RemoveSingleLineComments(
        string text
        , CommentSyntax syntax
        , string? keepFlag
    ) {
        if (syntax.SingleLine is null)
            return text;

        var token = syntax.SingleLine;
        var sb = new StringBuilder(text.Length);
        var lines = text.Split('\n');

        for (int idx = 0; idx < lines.Length; idx++)
        {
            var line = lines[idx];
            int pos = line.IndexOf(token, StringComparison.Ordinal);
            if (pos < 0)
            {
                sb.Append(line);
            }
            else
            {
                var commentPart = line.Substring(pos);
                bool keep =
                    !string.IsNullOrEmpty(keepFlag)
                    && commentPart.Contains(keepFlag, StringComparison.Ordinal);

                if (keep)
                {
                    sb.Append(line);
                }
                else
                {
                    sb.Append(line.Substring(0, pos));
                }
            }

            if (idx < lines.Length - 1)
                sb.Append('\n');
        }

        return sb.ToString();
    }
}
