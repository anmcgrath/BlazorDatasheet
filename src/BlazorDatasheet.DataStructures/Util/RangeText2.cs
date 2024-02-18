using BlazorDatasheet.DataStructures.References;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.DataStructures.Util;

public static class RangeText2
{
    public const int MaxCols = 100000;
    public const int MaxRows = 100000;

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
        var colNum = ColStrToNumber(colSpan);

        if (colNum < MaxCols && rowNum < MaxRows)
            cellReference = new CellReference(rowNum, colNum, isFixedCol, isFixedRow);
        else
            cellReference = new NamedReference(str.ToString());
        return true;
    }

    /// <summary>
    /// Tries to parse a reference of the form A1:A3 etc.
    /// This reference string expects zero or more colons as separators.
    /// There may be more than one part e.g A1:A2:A4
    /// </summary>
    /// <param name="refStr"></param>
    /// <param name="reference"></param>
    /// <returns></returns>
    public static bool TryParseReference(ReadOnlySpan<char> refStr, out Reference? reference)
    {
        var parsedRefs = new List<Reference>();

        int pos = 0;
        int start = 0;
        while (pos <= refStr.Length - 1)
        {
            // look for range strings between colons
            while (refStr[pos] != ':')
            {
                pos++;
                if (pos > refStr.Length - 1)
                    break;
            }


            int len = pos - start;

            if (TryParseSingleReference(refStr.Slice(start, len), out var parsedRef))
                parsedRefs.Add(parsedRef!);
            else
            {
                reference = null;
                return false;
            }

            pos++; // skip :
            start = pos;
        }

        if (parsedRefs.Count == 2)
        {
            reference = new RangeReference(parsedRefs[0], parsedRefs[1]);
            return true;
        }

        if (parsedRefs.Count == 1)
        {
            var singleRef = parsedRefs[0];

            // the only way a single reference is valid is if it's a named reference
            // that may be a column reference or a named reference
            if (singleRef.Kind == ReferenceKind.Named)
            {
                reference = singleRef;
                return true;
            }

            if (singleRef.Kind == ReferenceKind.Column)
            {
                var colRef = (ColReference)singleRef;
                reference = new NamedReference(ColNumberToLetters(colRef.ColNumber));
                return true;
            }

            reference = null;
            return false;
        }

        reference = new MultiReference(parsedRefs.ToArray());
        return true;
    }

    /// <summary>
    /// Returns a reference from a single part of the range string - there shouldn't be any colons in the string
    /// passed to this function.
    /// </summary>
    /// <param name="str"></param>
    /// <param name="reference"></param>
    /// <returns>Either a col reference, row reference, cell reference or named reference.</returns>
    public static bool TryParseSingleReference(ReadOnlySpan<char> str, out Reference? reference)
    {
        reference = null;
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

        int colNum = colStrLen == 0 ? -1 : ColStrToNumber(str.Slice(startColStr, colStrLen));

        // if there are no characters after col ref then it is a single col ref
        if (pos == str.Length)
        {
            if (colNum < MaxCols)
                reference = new ColReference(colNum, isFirstFixed);
            else
                reference = new NamedReference(str.Slice(startColStr, colStrLen).ToString());
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
        // if there are more chars after the row number it's got to be a named reference
        if (pos <= str.Length - 1)
        {
            if (!IsValidStartOfName(str[0]))
                return false;

            for (int i = 0; i < str.Length; i++)
            {
                if (!IsValidNameChar(str[i]))
                {
                    return false;
                }
            }

            reference = new NamedReference(str.ToString());
            return true;
        }

        if (rowStrLen == 0) // bad parse somehow - shouldn't be possible
            return false;

        int rowNum = int.Parse(str.Slice(startRowStr, rowStrLen)) - 1;
        if (rowNum > MaxRows)
        {
            reference = new NamedReference(str.ToString());
            return true;
        }

        if (hasColRef)
        {
            reference = new CellReference(rowNum, colNum, isFirstFixed, isSecondFixed);
            return true;
        }

        reference = new RowReference(rowNum, isFirstFixed);
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


    private static int ColStrToNumber(ReadOnlySpan<char> text)
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
}