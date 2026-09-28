using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.RowCols;

public class SetHeadingAlignmentCommand : BaseCommand, IUndoableCommand
{
    private readonly int _indexStart;
    private readonly int _indexEnd;
    private readonly TextAlign? _alignment;
    private readonly Axis _axis;
    private RowColInfoRestoreData _restoreData = null!;

    public SetHeadingAlignmentCommand(int indexStart, int indexEnd, TextAlign? alignment, Axis axis)
    {
        _indexStart = indexStart;
        _indexEnd = indexEnd;
        _alignment = alignment;
        _axis = axis;
    }

    protected override bool ExecuteCore(Sheet sheet)
    {
        _restoreData = sheet.GetRowColStore(_axis).SetHeadingAlignmentImpl(_indexStart, _indexEnd, _alignment);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.GetRowColStore(_axis).Restore(_restoreData);
        return true;
    }
}
