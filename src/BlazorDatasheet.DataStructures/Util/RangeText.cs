using System.Text.RegularExpressions;

namespace BlazorDatasheet.DataStructures.Util;

public class RangeText
{
    private static readonly string CELL_REFERENCE_REGEX = @"\$*[a-zA-Z]+\$*[0-9]+";
    private static Regex _regex = new Regex(CELL_REFERENCE_REGEX);

    public static bool IsValidColReference(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '$' && i == 0)
                continue;
            if (!char.IsLetter(c))
                return false;
        }

        return true;
    }

    public static bool IsValidRowReference(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '$' && i == 0)
                continue;
            if (!char.IsDigit(c))
                return false;
        }

        return true;
    }

    public static bool IsValidCellReference(string text)
    {
        var match = _regex.Match(text);
        return match.Success && match.Value == text;
    }


    public static int RowStrToNumber(string rowText)
    {
        if (rowText.StartsWith('$'))
            return int.Parse(rowText.Substring(1, rowText.Length - 1)) - 1;
        return int.Parse(rowText) - 1;
    }

    public static int ColStrToNumber(string text)
    {
        var str = text.ToUpper();
        var start = 'A';
        var result = 0;
        var i0 = 0;

        if (str.First() == '$')
            i0 = 1;

        for (int i = i0; i < str.Length; i++)
        {
            result *= 26;
            var offset = (int)str[i] - (int)start + 1;
            result += offset;
        }

        return result - 1;
    }

    public static string ColNumberToLetters(int n)
    {
        var N = n + 1;
        string str = "";
        while (N > 0)
        {
            int m = (N - 1) % 26;
            str = Convert.ToChar('A' + m) + str;
            N = (N - m) / 26;
        }

        return str;
    }

    public static (int row, int col, bool fixedRow, bool fixedCol) CellFromString(string text)
    {
        bool fixedCol = false;
        bool fixedRow = false;

        int index = 0;
        if (text[index] == '$')
        {
            fixedCol = true;
            index++;
        }

        string colText = "";

        while (index < text.Length &&
               char.IsLetter(text[index]))
        {
            colText += text[index];
            index++;
        }

        if (text[index] == '$')
        {
            fixedRow = true;
            index++;
        }

        string rowText = "";

        while (index < text.Length &&
               char.IsNumber(text[index]))
        {
            rowText += text[index];
            index++;
        }

        var row = RangeText.RowStrToNumber(rowText);
        var col = RangeText.ColStrToNumber(colText);

        return (row, col, fixedRow, fixedCol);
    }
}