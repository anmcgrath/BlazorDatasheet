using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;

namespace BlazorDatasheet.Core.Commands.RowCols;

public class HideCommand : BaseCommand, IUndoableCommand
{
    private readonly Axis _axis;

    private List<Interval> _intervals;

    private RowColInfoRestoreData _restoreData = null!;

    public HideCommand(int start, int end, Axis axis)
    {
        _intervals = [new(start, end)];
        _axis = axis;
    }

    public HideCommand(List<Interval> intervals, Axis axis)
    {
        _intervals = intervals;
        _axis = axis;
    }

    public override bool CanExecute(Sheet sheet) => true;

    public override bool Execute(Sheet sheet)
    {
        _restoreData = sheet.GetRowColStore(_axis).HideImpl(_intervals);
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