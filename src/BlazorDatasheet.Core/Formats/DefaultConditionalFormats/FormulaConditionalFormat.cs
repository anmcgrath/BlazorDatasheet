using System.Text.Json.Serialization;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Core.Formats.DefaultConditionalFormats;

/// <summary>
/// A conditional format that applies <see cref="Format"/> to a cell when a formula evaluates to true
/// for that cell. The formula is written relative to the anchor (top left of the applied range) and is
/// re-based for every other cell in the range, honouring $ fixed flags - the same as Excel's
/// "use a formula to determine which cells to format".
/// </summary>
public class FormulaConditionalFormat : ConditionalFormatAbstractBase
{
    private string _formulaText;
    private CellFormula? _cellFormula;

    [JsonConstructor]
    public FormulaConditionalFormat(string formula, CellFormat format)
    {
        _formulaText = formula;
        Format = format;
        IsShared = true;
        Predicate = (posn, sheet) => Evaluate(posn, sheet);
    }

    /// <summary>
    /// The formula text, as currently stored (references may have been rewritten by row/column edits).
    /// </summary>
    public string Formula => _cellFormula?.ToFormulaString() ?? _formulaText;

    /// <summary>
    /// The format applied when the formula is true.
    /// </summary>
    public CellFormat Format { get; }

    /// <summary>
    /// The cell the formula is written relative to. Maintained by the <see cref="ConditionalFormatManager"/>.
    /// </summary>
    internal CellPosition Anchor { get; set; }

    internal string FormulaText => Formula;

    internal IEnumerable<Reference> References =>
        _cellFormula?.References ?? Enumerable.Empty<Reference>();

    private CellFormula? GetFormula(Sheet sheet)
    {
        if (_cellFormula == null)
        {
            var text = _formulaText;
            if (string.IsNullOrWhiteSpace(text))
                return null;
            _cellFormula = sheet.FormulaEngine.ParseFormula(text, sheet.Name);
        }

        return _cellFormula;
    }

    private bool Evaluate(CellPosition posn, Sheet sheet)
    {
        var formula = GetFormula(sheet);
        if (formula == null)
            return false;

        return IsTruthy(sheet.FormulaEngine.EvaluateFormulaAt(formula, Anchor, posn.row, posn.col, sheet.Name));
    }

    private static bool IsTruthy(CellValue value)
    {
        return value.ValueType switch
        {
            CellValueType.Logical => value.LogicalValue,
            CellValueType.Number => value.NumberValue != 0,
            _ => false
        };
    }

    public override CellFormat? CalculateFormat(int row, int col, Sheet sheet)
    {
        // the manager merges into whatever is returned here, so it must not be the stored instance.
        return Format.Clone();
    }

    public override ConditionalFormatAbstractBase Clone()
    {
        return new FormulaConditionalFormat(Formula, Format.Clone())
        {
            Anchor = Anchor,
            IsShared = IsShared,
            Order = Order
        };
    }

    internal void EnsureParsed(Sheet sheet) => GetFormula(sheet);

    internal void SetFormulaText(string text, Sheet sheet)
    {
        _formulaText = text;
        _cellFormula = null;
        GetFormula(sheet);
    }

    internal void ShiftRelative(int dRow, int dCol, Sheet sheet)
    {
        if (dRow == 0 && dCol == 0)
            return;
        GetFormula(sheet)?.ShiftReferences(dRow, dCol, sheet.Name);
    }

    internal void InsertRowCol(int index, int count, Axis axis, Sheet sheet)
    {
        GetFormula(sheet)?.InsertRowColIntoReferences(index, count, axis, sheet.Name);
    }

    internal void RemoveRowCol(int index, int count, Axis axis, Sheet sheet)
    {
        GetFormula(sheet)?.RemoveRowColFromReferences(index, count, axis, sheet.Name);
    }
}
