using System.Text;

namespace BlazorDatasheet.Core.Clipboard;

public static class DelimitedTextWriter
{
    /// <summary>
    /// Writes the snapshot as delimited text the way Excel does: a field is quoted only when it contains the
    /// delimiter, a quote or a line break, and quotes inside it are doubled.
    /// </summary>
    public static string Write(ClipboardSnapshot snapshot, char delimiter = '\t', string newLine = "\r\n")
    {
        var sb = new StringBuilder();
        for (var r = 0; r < snapshot.Height; r++)
        {
            if (r > 0)
                sb.Append(newLine);
            for (var c = 0; c < snapshot.Width; c++)
            {
                if (c > 0)
                    sb.Append(delimiter);
                var cell = snapshot[r, c];
                var text = cell == null ? string.Empty :
                    cell.RawText ?? ClipboardValueText.Display(cell.Value);
                sb.Append(Quote(text, delimiter));
            }
        }

        return sb.ToString();
    }

    public static string Quote(string text, char delimiter)
    {
        if (text.IndexOf(delimiter) < 0 && text.IndexOf('"') < 0 && text.IndexOf('\n') < 0 && text.IndexOf('\r') < 0)
            return text;
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }
}
