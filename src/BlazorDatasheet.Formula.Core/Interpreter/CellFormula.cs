using System.Runtime.CompilerServices;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core.Interpreter.Parsing;
using BlazorDatasheet.Formula.Core.Interpreter.References;

[assembly: InternalsVisibleTo("BlazorDatasheet.Test")]

namespace BlazorDatasheet.Formula.Core.Interpreter;

public class CellFormula
{
    public bool ContainsVolatiles { get; }
    internal readonly SyntaxTree ExpressionTree;
    public IEnumerable<Reference> References => ExpressionTree.References;

    internal CellFormula(SyntaxTree expressionTree, bool containsVolatiles = false)
    {
        ContainsVolatiles = containsVolatiles;
        ExpressionTree = expressionTree;
    }

    public bool IsValid()
    {
        return !ExpressionTree.Errors.Any();
    }

    /// <summary>
    /// Returns whether the formula consists solely of a cell or range reference, optionally wrapped
    /// in parentheses. Invalidated references still count as reference formulas.
    /// </summary>
    public bool IsReferenceFormula()
    {
        Expression expression = ExpressionTree.Root;
        while (expression is ParenthesizedExpression parenthesized)
            expression = parenthesized.Expression;
        return expression is ReferenceExpression;
    }

    public string ToFormulaString(bool includeEquals = true)
    {
        return includeEquals ? $"={ExpressionTree.Root.ToExpressionText()}" : ExpressionTree.Root.ToExpressionText();
    }

    /// <summary>
    /// Shifts all references by <paramref name="offsetRow"/> rows and <paramref name="offsetCol"/> columns.
    /// References are only shifted if the sheet name matches <paramref name="sheetName"/>.
    /// </summary>
    /// <param name="offsetRow"></param>
    /// <param name="offsetCol"></param>
    /// <param name="sheetName"></param>
    public void ShiftReferences(int offsetRow, int offsetCol, string? sheetName)
    {
        foreach (var reference in References)
        {
            if (sheetName != null && reference.SheetName != sheetName)
                continue;
            reference.Shift(offsetRow, offsetCol);
        }
    }

    public void InsertRowColIntoReferences(int index, int count, Axis axis, string sheetName)
    {
        foreach (var reference in References)
        {
            if (reference.SheetName != sheetName)
                continue;

            if (reference is CellReference cellReference)
            {
                if (axis == Axis.Row && cellReference.RowIndex >= index)
                    reference.Move(count, 0);
                else if (axis == Axis.Col && cellReference.ColIndex >= index)
                    reference.Move(0, count);
            }

            if (reference is RangeReference)
            {
                if (axis == Axis.Row && reference.Region.Top >= index)
                    reference.Move(count, 0);
                else if (axis == Axis.Col && reference.Region.Left >= index)
                    reference.Move(0, count);

                if (axis == Axis.Row && reference.Region.SpansRow(index))
                    reference.Region.Expand(Edge.Bottom, count);

                if (axis == Axis.Col && reference.Region.SpansCol(index))
                    reference.Region.Expand(Edge.Right, count);
            }
        }
    }

    public void RemoveRowColFromReferences(int index, int count, Axis axis, string sheetName)
    {
        foreach (var reference in References)
        {
            if (reference.SheetName != sheetName)
                continue;

            var end = index + count - 1;
            IRegion removalRegion = axis == Axis.Col
                ? new ColumnRegion(index, end)
                : new RowRegion(index, end);

            if (removalRegion.Contains(reference.Region))
            {
                reference.SetValidity(false);
                continue;
            }

            var start = axis == Axis.Row ? reference.Region.Top : reference.Region.Left;
            var stop = axis == Axis.Row ? reference.Region.Bottom : reference.Region.Right;

            if (start > end)
            {
                if (axis == Axis.Row)
                    reference.Move(-count, 0);
                else
                    reference.Move(0, -count);
                continue;
            }

            if (stop < index || reference is not RangeReference)
                continue;

            // The range overlaps the removed band without being inside it: it loses the
            // overlapping rows/cols, and a range that began inside the band now begins at it.
            var overlap = Math.Min(stop, end) - Math.Max(start, index) + 1;
            var moveBy = Math.Min(start, index) - start;

            if (axis == Axis.Row)
            {
                reference.Region.Contract(Edge.Bottom, overlap);
                reference.Move(moveBy, 0);
            }
            else
            {
                reference.Region.Contract(Edge.Right, overlap);
                reference.Move(0, moveBy);
            }
        }
    }
}
