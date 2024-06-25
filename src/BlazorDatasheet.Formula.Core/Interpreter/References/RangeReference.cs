using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Formula.Core.Interpreter.References;

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

            ColReference colStart, colEnd;
            RowReference rowStart, rowEnd;

            if (cellStart.Col.ColNumber <= cellEnd.Col.ColNumber)
            {
                colStart = cellStart.Col;
                colEnd = cellEnd.Col;
            }
            else
            {
                colStart = cellEnd.Col;
                colEnd = cellStart.Col;
            }

            if (cellStart.Row.RowNumber <= cellEnd.Row.RowNumber)
            {
                rowStart = cellStart.Row;
                rowEnd = cellEnd.Row;
            }
            else
            {
                rowStart = cellEnd.Row;
                rowEnd = cellStart.Row;
            }

            Start = new CellReference(rowStart, colStart);
            End = new CellReference(rowEnd, colEnd);
            return;
        }

        Start = start;
        End = end;
    }

    protected RangeReference()
    {
    }

    public override string ToAddressText()
    {
        return Start.ToAddressText() + ":" + End.ToAddressText();
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

    public override void Shift(int offsetRow, int offsetCol)
    {
        Start.Shift(offsetRow, offsetCol);
        End.Shift(offsetRow, offsetCol);
    }

    public override bool IsInvalid { get; protected set; }

    public override IRegion ToRegion()
    {
        return Start.ToRegion().GetBoundingRegion(End.ToRegion());
    }

    public override string ToString() => ToAddressText();
}