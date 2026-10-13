using System.Text;

namespace BlazorDatasheet.Core.Clipboard;

public static class DelimitedTextParser
{
    /// <summary>
    /// Splits delimited text into rows of fields. A field that starts with a quote runs to the closing quote
    /// and may contain the delimiter and line breaks; a doubled quote inside it is one quote. Rows may be
    /// ragged. One trailing line break is ignored.
    /// </summary>
    public static List<List<string>> Parse(string? text, char delimiter = '\t')
    {
        var rows = new List<List<string>>();
        if (string.IsNullOrEmpty(text))
            return rows;

        var row = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var i = 0;
        while (i < text.Length)
        {
            var ch = text[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i += 2;
                        continue;
                    }

                    inQuotes = false;
                    i++;
                    continue;
                }

                field.Append(ch);
                i++;
                continue;
            }

            if (ch == '"' && field.Length == 0)
            {
                inQuotes = true;
                i++;
            }
            else if (ch == delimiter)
            {
                row.Add(field.ToString());
                field.Clear();
                i++;
            }
            else if (ch == '\r' || ch == '\n')
            {
                row.Add(field.ToString());
                field.Clear();
                rows.Add(row);
                row = new List<string>();
                i += ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n' ? 2 : 1;
            }
            else
            {
                field.Append(ch);
                i++;
            }
        }

        if (field.Length > 0 || row.Count > 0 || inQuotes)
        {
            row.Add(field.ToString());
            rows.Add(row);
        }

        return rows;
    }

    /// <summary>
    /// Parses delimited text into a snapshot of untyped cells.
    /// </summary>
    public static ClipboardSnapshot ToSnapshot(string? text, char delimiter = '\t')
    {
        var rows = Parse(text, delimiter);
        var width = rows.Count == 0 ? 0 : rows.Max(r => r.Count);
        var snapshot = new ClipboardSnapshot(rows.Count, width);
        for (var r = 0; r < rows.Count; r++)
        for (var c = 0; c < rows[r].Count; c++)
        {
            if (rows[r][c].Length > 0)
                snapshot[r, c] = new ClipboardCell { RawText = rows[r][c] };
        }

        return snapshot;
    }
}
