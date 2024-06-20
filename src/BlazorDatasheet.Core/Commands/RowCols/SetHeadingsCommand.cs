using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.RowCols;

public class SetHeadingsCommand : IUndoableCommand
{
    private readonly int _indexStart;
    private readonly int _indexEnd;
    private readonly string _heading;
    private readonly Axis _axis;
    private RowColInfoRestoreData _restoreData = null!;

    public SetHeadingsCommand(int indexStart, int indexEnd, string heading, Axis axis)
    {
        _indexStart = indexStart;
        _indexEnd = indexEnd;
        _heading = heading;
        _axis = axis;
    }

    public bool Execute(Sheet sheet)
    {
        _restoreData = sheet.GetRowColStore(_axis).SetHeadingsImpl(_indexStart, _indexEnd, _heading);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.GetRowColStore(_axis).Restore(_restoreData);
        return true;
    }
}