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

    /// <summary>
    /// Moves the references that lie fully inside <paramref name="region"/> on sheet
    /// <paramref name="regionSheetName"/> by <paramref name="dRow"/> rows and <paramref name="dCol"/>
    /// columns, as when the referenced cells themselves are cut and pasted elsewhere. Fixed ($) flags are
    /// ignored because the cells moved. References that only partly overlap the region, references to
    /// other sheets and named references are left alone (Excel semantics).
    /// </summary>
    /// <param name="region">The region of cells that moved.</param>
    /// <param name="regionSheetName">The sheet the moved cells came from.</param>
    /// <param name="dRow">Row offset of the move.</param>
    /// <param name="dCol">Column offset of the move.</param>
    /// <param name="destinationSheetName">The sheet the cells moved to.</param>
    /// <param name="formulaSheetName">The sheet this formula lives on. A moved reference is written with an
    /// explicit sheet name when the destination is a different sheet to this one.</param>
    /// <returns>Whether any reference was changed.</returns>
    public bool MoveReferencesWithin(IRegion region, string regionSheetName, int dRow, int dCol,
        string destinationSheetName, string formulaSheetName)
    {
        var moved = false;
        foreach (var reference in References)
        {
            if (reference.Kind == ReferenceKind.Named || reference.IsInvalid)
                continue;
            if (reference.SheetName != regionSheetName || !region.Contains(reference.Region))
                continue;

            reference.Move(dRow, dCol);
            var explicitName = reference.ExplicitSheetName || destinationSheetName != formulaSheetName;
            reference.SetSheetName(destinationSheetName, explicitName);
            moved = true;
        }

        return moved;
    }

    /// <summary>
    /// Gives every reference to a sheet other than <paramref name="formulaSheetName"/> an explicit sheet
    /// name, so the formula text stays unambiguous when the formula lives on that sheet.
    /// </summary>
    public void QualifyForeignReferences(string formulaSheetName)
    {
        foreach (var reference in References)
        {
            if (reference.Kind == ReferenceKind.Named || reference.ExplicitSheetName)
                continue;
            if (reference.SheetName != formulaSheetName)
                reference.SetSheetName(reference.SheetName, true);
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
