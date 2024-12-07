using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core.Interpreter.Addresses;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core;

public static class RangeText
{
    public const int MaxCols = 16384;
    public const int MaxRows = 1048576;

    /// <summary>
    /// Returns a reference from a single part of the range string - there shouldn't be any colons in the string
    /// passed to this function.
    /// </summary>
    /// <param name="str"></param>
    /// <param name="address"></param>
    /// <returns>Either a col reference, row reference, cell reference or named reference.</returns>
    public static bool TryParseSingleAddress(ReadOnlySpan<char> str, out Address? address)
    {
        address = null;
        if (str.Length < 1)
            return false;

        // will be either 1. a cell reference 2. a single column reference or 3. a single row reference
        bool hasColRef = false;
        bool isFirstFixed = false;
        bool isSecondFixed = false;

        int pos = 0;
        if (str[0] == '$')
        {
            isFirstFixed = true;
            pos++;
        }

        int startColStr = pos;
        while (pos <= str.Length - 1 && char.IsLetter(str[pos]))
            pos++;

        int colStrLen = pos - startColStr;
        if (colStrLen != 0)
            hasColRef = true;

        var colStrSpan = str.Slice(startColStr, colStrLen);
        int colIndex = colStrLen == 0 ? -1 : ColStrToIndex(colStrSpan);

        // if there are no characters after col ref then it is a single col ref
        if (pos == str.Length)
        {
            if (colIndex < MaxCols)
                address = new ColAddress(colIndex, colStrSpan.ToString(), isFirstFixed);
            else
                address = new NamedAddress(colStrSpan.ToString(), IsValidNameAddress(colStrSpan));
            return true;
        }

        if (str[pos] == '$')
        {
            isSecondFixed = true;
            pos++;
        }

        if (pos > str.Length - 1)
        {
            if (isSecondFixed) // row ends with $ - invalid
                return false;
        }

        int startRowStr = pos;
        while (pos <= str.Length - 1 && char.IsDigit(str[pos]))
            pos++;

        int rowStrLen = pos - startRowStr;
        // if there are more chars after the row number it's got to be invalid
        if (pos <= str.Length - 1)
        {
            return false;
        }

        if (rowStrLen == 0) // bad parse somehow - shouldn't be possible
            return false;

        int rowIndex = int.Parse(str.Slice(startRowStr, rowStrLen)) - 1;
        if (rowIndex > MaxRows)
        {
            address = new NamedAddress(str.ToString(), IsValidNameAddress(str));
            return true;
        }

        if (hasColRef)
        {
            var colAddress = new ColAddress(colIndex, colStrSpan.ToString(), isFirstFixed);
            var rowAddress = new RowAddress(rowIndex, rowIndex + 1, isSecondFixed);
            address = new CellAddress(rowAddress, colAddress);
            return true;
        }

        address = new RowAddress(rowIndex, rowIndex + 1, isFirstFixed);
        return true;
    }

    public static bool IsValidNameAddress(ReadOnlySpan<char> addrSpan)
    {
        foreach (var c in addrSpan)
        {
            if (!IsValidNameChar(c))
                return false;
        }

        return true;
    }

    private static bool IsValidNameChar(char c)
    {
        return char.IsLetterOrDigit(c) ||
               c == '_' ||
               c == '?' ||
               c == '.' ||
               c == '\\';
    }

    private static bool IsValidStartOfName(char c)
    {
        return char.IsLetter(c) || c == '_';
    }


    private static int ColStrToIndex(ReadOnlySpan<char> text)
    {
        var col0 = 'A';
        var result = 0;

        for (int i = 0; i < text.Length; i++)
        {
            result *= 26;
            var c = char.ToUpper(text[i]);
            var offset = c - col0 + 1;
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

    public static string ToCellText(int row, int col)
    {
        return $"{ColNumberToLetters(col)}{row + 1}";
    }

    private static string FS(bool isFixed) => isFixed ? "$" : string.Empty;

    public static string ToRegionText(IRegion region,
        bool fixedColStart = false,
        bool fixeColEnd = false,
        bool fixedRowStart = false,
        bool fixedRowEnd = false)
    {
        var firstCol = ColNumberToLetters(region.Left);
        var firstRow = (region.Top + 1).ToString();

        if (region.Width == 1 && region.Height == 1)
            return $"{FS(fixedColStart)}{firstCol}{FS(fixedRowStart)}{firstRow}";

        var isRowRegion = region is RowRegion;
        var isColRegion = region is ColumnRegion;

        firstCol = isColRegion || (!isRowRegion && !isColRegion) ? firstCol : string.Empty;
        firstRow = isRowRegion || (!isRowRegion && !isColRegion) ? firstRow : string.Empty;

        var lastCol = isColRegion || (!isRowRegion && !isColRegion) ? ColNumberToLetters(region.Right) : string.Empty;
        var lastRow = isRowRegion || (!isRowRegion && !isColRegion) ? (region.Bottom + 1).ToString() : string.Empty;
        return
            $"{FS(fixedColStart)}{firstCol}{FS(fixedRowStart)}{firstRow}:" +
            $"{FS(fixeColEnd)}{lastCol}{FS(fixedRowEnd)}{lastRow}";
    }
}