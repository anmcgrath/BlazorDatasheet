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
            if (colIndex < MaxCols && isFirstFixed)
                address = new ColAddress(colIndex, isFirstFixed);
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

        if (!int.TryParse(str.Slice(startRowStr, rowStrLen), out var rowIndex))
            return false;
        rowIndex--;
        if (rowIndex > MaxRows)
        {
            address = new NamedAddress(str.ToString());
            return true;
        }

        if (hasColRef)
        {
            var colAddress = new ColAddress(colIndex, isFirstFixed);
            var rowAddress = new RowAddress(rowIndex, rowIndex + 1, isSecondFixed);
            address = new CellAddress(rowAddress, colAddress);
            return true;
        }

        address = new RowAddress(rowIndex, rowIndex + 1, isFirstFixed);
        return true;
    }

    public static bool IsValidNameChar(char c)
    {
        return char.IsLetterOrDigit(c) ||
               c == '_' ||
               c == '?' ||
               c == '.' ||
               c == '\\';
    }

    public static bool IsValidStartOfName(char c)
    {
        return char.IsLetter(c) || c == '_';
    }

    public static bool IsValidName(ReadOnlySpan<char> name)
    {
        if (name.Length == 0)
            return false;

        if (!IsValidStartOfName(name[0]))
            return false;

        for (int i = 1; i < name.Length; i++)
        {
            if (!IsValidNameChar(name[i]))
                return false;
        }

        return true;
    }


    public static int ColStrToIndex(ReadOnlySpan<char> text)
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

    public static string ColIndexToLetters(int colIndex)
    {
        var n = colIndex + 1;
        int strLength = n <= 26 ? 1 : (n > 702 ? 3 : 2); // we only support columns up to 16384
        char[] letters = new char[strLength];
        int i = 0;

        while (n > 0)
        {
            int m = (n - 1) % 26;
            letters[letters.Length - 1 - i] = Convert.ToChar('A' + m);
            n = (n - m) / 26;
            i++;
        }

        return new string(letters);
    }

    public static string ToCellText(int row, int col)
    {
        return $"{ColIndexToLetters(col)}{row + 1}";
    }

    private static string FixedString(bool isFixed) => isFixed ? "$" : string.Empty;

    public static string RegionToText(IRegion region,
        bool fixedColStart = false,
        bool fixeColEnd = false,
        bool fixedRowStart = false,
        bool fixedRowEnd = false)
    {
        var firstCol = ColIndexToLetters(region.Left);
        var firstRow = (region.Top + 1).ToString();

        if (region.Width == 1 && region.Height == 1)
            return $"{FixedString(fixedColStart)}{firstCol}{FixedString(fixedRowStart)}{firstRow}";

        var isRowRegion = region is RowRegion;
        var isColRegion = region is ColumnRegion;

        firstCol = isColRegion || (!isRowRegion && !isColRegion) ? firstCol : string.Empty;
        firstRow = isRowRegion || (!isRowRegion && !isColRegion) ? firstRow : string.Empty;

        var lastCol = isColRegion || (!isRowRegion && !isColRegion) ? ColIndexToLetters(region.Right) : string.Empty;
        var lastRow = isRowRegion || (!isRowRegion && !isColRegion) ? (region.Bottom + 1).ToString() : string.Empty;
        return
            $"{FixedString(fixedColStart)}{firstCol}{FixedString(fixedRowStart)}{firstRow}:" +
            $"{FixedString(fixeColEnd)}{lastCol}{FixedString(fixedRowEnd)}{lastRow}";
    }
}