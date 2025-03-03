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

    internal void InsertRowColIntoReferences(int index, int count, Axis axis, string sheetName)
    {
        foreach (var reference in References)
        {
            if (reference.SheetName != sheetName)
                continue;

            if (reference is CellReference cellReference)
            {
                if (axis == Axis.Row && cellReference.RowIndex >= index)
                    reference.Shift(count, 0);
                else if (axis == Axis.Col && cellReference.ColIndex >= index)
                    reference.Shift(0, count);
            }

            if (reference is RangeReference rangeReference)
            {
                if (axis == Axis.Row && reference.Region.Top >= index)
                    reference.Shift(count, 0);
                else if (axis == Axis.Col && reference.Region.Left >= index)
                    reference.Shift(0, count);

                if (axis == Axis.Row && reference.Region.SpansRow(index))
                    reference.Region.Expand(Edge.Bottom, count);

                if (axis == Axis.Col && reference.Region.SpansCol(index))
                    reference.Region.Expand(Edge.Right, count);
            }
        }
    }

    internal void RemoveRowColFromReferences(int index, int count, Axis axis, string sheetName)
    {
        foreach (var reference in References)
        {
            if (reference.SheetName != sheetName)
                continue;

            if (axis == Axis.Row && reference.Region.Top > index)
            {
                reference.Shift(-count, 0);
                continue;
            }

            if (axis == Axis.Col && reference.Region.Left > index)
            {
                reference.Shift(0, -count);
                continue;
            }

            IRegion removalRegion = axis == Axis.Col
                ? new ColumnRegion(index, index + count - 1)
                : new RowRegion(index, index + count - 1);

            if (removalRegion.Contains(reference.Region))
            {
                reference.SetValidity(false);
                continue;
            }

            if (reference is RangeReference rangeReference)
            {
                if (axis == Axis.Row && reference.Region.SpansRow(index))
                    reference.Region.Contract(Edge.Bottom, count);

                if (axis == Axis.Col && reference.Region.SpansCol(index))
                    reference.Region.Contract(Edge.Right, count);
            }
        }
    }
}