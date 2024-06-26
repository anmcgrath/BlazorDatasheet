using System.Runtime.CompilerServices;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core.Interpreter.Parsing;
using BlazorDatasheet.Formula.Core.Interpreter.References;

[assembly: InternalsVisibleTo("BlazorDatasheet.Test")]

namespace BlazorDatasheet.Formula.Core.Interpreter;

public class CellFormula
{
    internal readonly SyntaxTree ExpressionTree;
    public IEnumerable<Reference> References => ExpressionTree.References;

    internal CellFormula(SyntaxTree expressionTree)
    {
        ExpressionTree = expressionTree;
    }

    public bool IsValid()
    {
        return !ExpressionTree.Errors.Any();
    }

    public string ToFormulaString() => "=" + ExpressionTree.Root.ToExpressionText();

    public void ShiftReferences(int offsetRow, int offsetCol)
    {
        foreach (var reference in References)
        {
            reference.Shift(offsetRow, offsetCol);
        }
    }

    internal void InsertRowColIntoReferences(int index, int count, Axis axis)
    {
        foreach (var reference in References)
        {
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

    internal void RemoveRowColFromReferences(int index, int count, Axis axis)
    {
        foreach (var reference in References)
        {
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

    public CellFormula Clone()
    {
        var parser = new Parser();
        return new CellFormula(parser.Parse(ToFormulaString()));
    }
}