using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.RowCols;

public class HideCommand : IUndoableCommand
{
    private int _start;
    private int _end;
    private readonly Axis _axis;

    private RowColInfoRestoreData _restoreData = null!;

    public HideCommand(int start, int end, Axis axis)
    {
        _start = start;
        _end = end;
        _axis = axis;
    }

    public bool Execute(Sheet sheet)
    {
        _restoreData = sheet.GetRowColStore(_axis).HideImpl(_start, _end - _start + 1);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.GetRowColStore(_axis).Restore(_restoreData);
        IRegion dirtyRegion = _axis == Axis.Col
            ? new ColumnRegion(0, int.MaxValue)
            : new RowRegion(0, int.MaxValue);
        sheet.MarkDirty(dirtyRegion);
        return true;
    }
}