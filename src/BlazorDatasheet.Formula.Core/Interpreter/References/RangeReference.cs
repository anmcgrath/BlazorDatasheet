using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core.Interpreter.Addresses;

namespace BlazorDatasheet.Formula.Core.Interpreter.References;

public class RangeReference : Reference
{
    public override ReferenceKind Kind => ReferenceKind.Range;

    public bool IsStartColFixed { get; private set; }
    public bool IsEndColFixed { get; private set; }
    public bool IsStartRowFixed { get; private set; }
    public bool IsEndRowFixed { get; private set; }

    public RangeReference(ColAddress colStart, ColAddress colEnd)
    {
        var start = colStart.ColIndex < colEnd.ColIndex ? colStart : colEnd;
        var end = start == colStart ? colEnd : colStart;

        Region = new ColumnRegion(start.ColIndex, end.ColIndex);
        IsStartColFixed = start.IsFixed;
        IsEndColFixed = end.IsFixed;
    }

    public RangeReference(RowAddress rowStart, RowAddress rowEnd)
    {
        var start = rowStart.RowIndex < rowEnd.RowIndex ? rowStart : rowEnd;
        var end = start == rowStart ? rowEnd : rowStart;

        Region = new RowRegion(start.RowIndex, end.RowIndex);
        IsStartRowFixed = start.IsFixed;
        IsEndRowFixed = end.IsFixed;
    }

    public RangeReference(CellAddress start, CellAddress end)
    {
        var cellStart = start;
        var cellEnd = end;

        ColAddress colStart, colEnd;
        RowAddress rowStart, rowEnd;

        if (cellStart.ColAddress.ColIndex <= cellEnd.ColAddress.ColIndex)
        {
            colStart = cellStart.ColAddress;
            colEnd = cellEnd.ColAddress;
        }
        else
        {
            colStart = cellEnd.ColAddress;
            colEnd = cellStart.ColAddress;
        }

        if (cellStart.RowAddress.RowIndex <= cellEnd.RowAddress.RowIndex)
        {
            rowStart = cellStart.RowAddress;
            rowEnd = cellEnd.RowAddress;
        }
        else
        {
            rowStart = cellEnd.RowAddress;
            rowEnd = cellStart.RowAddress;
        }

        IsStartColFixed = colStart.IsFixed;
        IsEndColFixed = colEnd.IsFixed;
        IsStartRowFixed = rowStart.IsFixed;
        IsEndRowFixed = rowEnd.IsFixed;

        Region = new Region(rowStart.RowIndex, rowEnd.RowIndex, colStart.ColIndex, colEnd.ColIndex);
    }

    protected RangeReference()
    {
    }

    public override string ToAddressText()
    {
        return RangeText.ToRegionText(Region, IsStartColFixed, IsEndColFixed, IsStartRowFixed, IsEndRowFixed);
    }

    public override bool SameAs(Reference reference)
    {
        if (reference.Kind == ReferenceKind.Range)
        {
            var rangeRef = (RangeReference)reference;
            return rangeRef.Region.Equals(Region);
        }

        if (reference.Kind == ReferenceKind.Cell)
        {
            var cellRef = (CellReference)reference;
            return cellRef.Region.Equals(Region);
        }

        return false;
    }

    public override void Shift(int offsetRow, int offsetCol)
    {
        var dRowStart = IsStartRowFixed ? 0 : offsetRow;
        var dRowEnd = IsEndRowFixed ? 0 : offsetRow;
        var dColStart = IsStartColFixed ? 0 : offsetCol;
        var dColEnd = IsEndColFixed ? 0 : offsetCol;
        Region.Shift(dRowStart, dRowEnd, dColStart, dColEnd);
    }

    public override bool IsInvalid { get; protected set; }
    public sealed override IRegion Region { get; protected set; }

    internal override void SetRegion(IRegion region)
    {
        Region = region;
    }

    public override string ToString() => ToAddressText();
}