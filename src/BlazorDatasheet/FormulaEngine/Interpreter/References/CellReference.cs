using System.Text.RegularExpressions;
using BlazorDatasheet.DataStructures.RTree;

namespace BlazorDatasheet.FormulaEngine.Interpreter.References;

public class CellReference : Reference
{
    private static readonly string CELL_REFERENCE_REGEX = @"\$*[A-z]+\$*[0-9]+";
    private static Regex _regex = new Regex(CELL_REFERENCE_REGEX);
    private readonly ColReference _col;
    private readonly RowReference _row;
    public ColReference Col => _col;
    public RowReference Row => _row;

    private Envelope _envelope;

    public CellReference(int row, int col, bool absoluteCol = false, bool absoluteRow = false) : this(
        new RowReference(row, absoluteRow), new ColReference(col, absoluteCol))
    {
    }

    public CellReference(RowReference row, ColReference col)
    {
        _row = row;
        _col = col;

        _envelope = new Envelope(_col.ColNumber, _row.RowNumber, _col.ColNumber, _row.RowNumber);
    }

    public static bool IsValid(string text)
    {
        var match = _regex.Match(text);
        return match.Success && match.Value == text;
    }

    public static CellReference? FromString(string text)
    {
        if (!IsValid(text))
            return null;

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

        var row = RowStrToNumber(rowText);
        var col = ColStrToNumber(colText);

        return new CellReference(row, col, fixedCol, fixedRow);
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

    public override ReferenceKind Kind { get; }

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

    public override string ToRefText()
    {
        return $"{Col.ToRefText()}{Row.ToRefText()}";
    }

    public override bool SameAs(Reference reference)
    {
        if (reference.Kind == ReferenceKind.Cell)
        {
            var cellRef = (CellReference)reference;
            return Col.SameAs(cellRef.Col) &&
                   Row.SameAs(cellRef.Row);
        }

        if (reference.Kind == ReferenceKind.Range)
        {
            var rangeRef = (RangeReference)reference;
            return this.SameAs(rangeRef.Start) &&
                   this.SameAs(rangeRef.End);
        }

        return false;
    }
}