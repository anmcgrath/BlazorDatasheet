using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.RowCols;

public class ClearHeadingGroupsCommand : BaseCommand, IUndoableCommand
{
    private readonly int _indexStart;
    private readonly int _indexEnd;
    private readonly Axis _axis;
    private RowColInfoRestoreData _restoreData = null!;

    public ClearHeadingGroupsCommand(int indexStart, int indexEnd, Axis axis)
    {
        _indexStart = indexStart;
        _indexEnd = indexEnd;
        _axis = axis;
    }

    public override bool CanExecute(Sheet sheet) => _indexStart <= _indexEnd;

    public override bool Execute(Sheet sheet)
    {
        _restoreData = sheet.GetRowColStore(_axis).ClearGroupsImpl(_indexStart, _indexEnd);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.GetRowColStore(_axis).Restore(_restoreData);
        return true;
    }
}
