using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.RowCols;

public class SetSizeCommand : IUndoableCommand
{
    private readonly Axis _axis;
    private int IndexStart { get; }
    public int IndexEnd { get; }
    private double Size { get; }
    private RowColInfoRestoreData _restoreData = null!;

    public SetSizeCommand(int indexStart, int indexEnd, double size, Axis axis)
    {
        _axis = axis;
        IndexStart = indexStart;
        IndexEnd = indexEnd;
        Size = size;
    }

    public bool Execute(Sheet sheet)
    {
        _restoreData = sheet.GetRowColStore(_axis).SetSizesImpl(IndexStart, IndexEnd, Size);
        IRegion dirtyRegion = _axis == Axis.Col
            ? new ColumnRegion(IndexStart, IndexEnd)
            : new RowRegion(IndexStart, IndexEnd);
        sheet.MarkDirty(dirtyRegion);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.GetRowColStore(_axis).Restore(_restoreData);
        IRegion dirtyRegion = _axis == Axis.Col
            ? new ColumnRegion(IndexStart, IndexEnd)
            : new RowRegion(IndexStart, IndexEnd);
        sheet.MarkDirty(dirtyRegion);
        return true;
    }
}