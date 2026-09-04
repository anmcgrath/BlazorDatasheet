using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.RowCols;

public class SetHeadingGroupCommand : BaseCommand, IUndoableCommand
{
    private readonly int _indexStart;
    private readonly int _indexEnd;
    private readonly string _label;
    private readonly Axis _axis;
    private RowColInfoRestoreData _restoreData = null!;

    public SetHeadingGroupCommand(int indexStart, int indexEnd, string label, Axis axis)
    {
        _indexStart = indexStart;
        _indexEnd = indexEnd;
        _label = label;
        _axis = axis;
    }

    public override bool CanExecute(Sheet sheet) => _indexStart <= _indexEnd;

    public override bool Execute(Sheet sheet)
    {
        _restoreData = sheet.GetRowColStore(_axis).SetGroupImpl(_indexStart, _indexEnd, _label);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.GetRowColStore(_axis).Restore(_restoreData);
        return true;
    }
}
