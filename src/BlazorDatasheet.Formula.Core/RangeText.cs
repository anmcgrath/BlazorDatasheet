using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core.Interpreter.Addresses;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core;

public static class RangeText
{
    public const int MaxCols = 16384;
    public const int MaxRows = 1048576;

    /// <summary>
    /// Parses a single cell reference.
    /// </summary>
    /// <param name="str"></param>
    /// <param name="cellReference"></param>
    /// <returns>Returns a CelLReference or NameReference</returns>
    public static bool TryParseSingleCellReference(ReadOnlySpan<char> str, out Reference? cellReference)
    {
        var isFixedCol = false;
        var isFixedRow = false;
        cellReference = null;

        // requires both a row and a col at the very least
        if (str.Length < 2)
            return false;

        int pos = 0;
        if (str[0] == '$')
        {
            isFixedCol = true;
            pos++;
        }
        // don't need to check pos is bound here because we know str length >=2
        // and we are checking it in the loop

        int startColStr = pos;
        while (pos <= str.Length - 1 && char.IsLetter(str[pos]))
            pos++;

        int colStrLen = pos - startColStr;
        if (colStrLen == 0)
        {
            cellReference = null;
            return false;
        }

        if (pos == str.Length)
            return false;

        if (str[pos] == '$')
        {
            isFixedRow = true;
            pos++;
        }

        if (pos > str.Length - 1)
        {
            if (!isFixedCol && !isFixedCol) // doesn't contain any $
            {
                cellReference = new NamedReference(str.ToString());
                return true;
            }

            return false;
        }

        int startRowStr = pos;
        while (pos <= str.Length - 1 && char.IsDigit(str[pos]))
            pos++;

        int rowStrLen = pos - startRowStr;
        // if there are more chars it's got to be a named string
        if (pos <= str.Length - 1)
        {
            cellReference = new NamedReference(str.ToString());
            return true;
        }

        if (rowStrLen == 0)
            return false;

        var colSpan = str.Slice(startColStr, colStrLen);
        var rowSpan = str.Slice(startRowStr, rowStrLen);

        var rowNum = int.Parse(rowSpan) - 1;
        var colNum = ColStrToIndex(colSpan);

        if (colNum < MaxCols && rowNum < MaxRows)
            cellReference = new CellReference(rowNum, colNum, isFixedCol, isFixedRow);
        else
            cellReference = new NamedReference(str.ToString());
        return true;
    }

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
                address = new NamedAddress(colStrSpan.ToString());
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
            address = new NamedAddress(str.ToString());
            return true;
        }

        if (hasColRef)
        {
            var colAddress = new ColAddress(colIndex, colStrSpan.ToString(), isFirstFixed);
            var rowAddress = new RowAddress(rowIndex, rowIndex + 1, isSecondFixed);
            address = new CellAddress(colAddress, rowAddress);
            return true;
        }

        address = new RowAddress(rowIndex, rowIndex + 1, isFirstFixed);
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

    public static string ToRegionText(IRegion region)
    {
        var firstCol = RangeText.ColNumberToLetters(region.Left);
        var firstRow = region.Top + 1;
        if (region.Left == region.Right && region.Top == region.Bottom)
            return $"{firstCol}{firstRow}";

        var lastCol = RangeText.ColNumberToLetters(region.Right);
        var lastRow = region.Bottom + 1;
        return $"{firstCol}{firstRow}:{lastCol}{lastRow}";
    }
}