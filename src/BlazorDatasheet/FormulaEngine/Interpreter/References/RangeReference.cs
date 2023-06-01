namespace BlazorDatasheet.FormulaEngine.Interpreter.References;

public class RangeReference : Reference
{
    public Reference Start { get; }
    public Reference End { get; }
    public override ReferenceKind Kind => ReferenceKind.Range;

    public RangeReference(Reference start, Reference end)
    {
        if (start.Kind == ReferenceKind.Cell && end.Kind == ReferenceKind.Cell)
        {
            var cellStart = (CellReference)start;
            var cellEnd = (CellReference)end;
            var colStart = cellStart.Col.ColNumber < cellEnd.Col.ColNumber ? cellStart.Col : cellEnd.Col;
            var colEnd = cellStart.Col.ColNumber > cellEnd.Col.ColNumber ? cellStart.Col : cellEnd.Col;
            var rowStart = cellStart.Row.RowNumber < cellEnd.Row.RowNumber ? cellStart.Row : cellEnd.Row;
            var rowEnd = cellStart.Row.RowNumber > cellEnd.Row.RowNumber ? cellStart.Row : cellEnd.Row;
            Start = new CellReference(rowStart, colStart);
            End = new CellReference(rowEnd, colEnd);
            return;
        }

        Start = start;
        End = end;
    }

    public override string ToRefText()
    {
        return Start.ToRefText() + ":" + End.ToRefText();
    }

    public override bool SameAs(Reference reference)
    {
        if (reference.Kind == ReferenceKind.Range)
        {
            var rangeRef = (RangeReference)reference;
            return Start.SameAs(rangeRef.Start) &&
                   End.SameAs(rangeRef.End);
        }

        if (reference.Kind == ReferenceKind.Cell)
        {
            var cellRef = (CellReference)reference;
            return Start.SameAs(End) &&
                   cellRef.SameAs(Start);
        }

        return false;
    }
}