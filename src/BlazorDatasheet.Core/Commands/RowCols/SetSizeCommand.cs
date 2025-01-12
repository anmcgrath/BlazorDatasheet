using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.RowCols;

public class SetSizeCommand : BaseCommand, IUndoableCommand
{
    private readonly Axis _axis;
    private readonly int _indexStart;
    private readonly int _indexEnd;
    private readonly double _size;
    private RowColInfoRestoreData _restoreData = null!;

    public SetSizeCommand(int indexStart, int indexEnd, double size, Axis axis)
    {
        _axis = axis;
        _indexStart = indexStart;
        _indexEnd = indexEnd;
        _size = size;
    }

    public override bool CanExecute(Sheet sheet) => _size >= 0;

    public override bool Execute(Sheet sheet)
    {
        _restoreData = sheet.GetRowColStore(_axis).SetSizesImpl(_indexStart, _indexEnd, _size);
        IRegion dirtyRegion = _axis == Axis.Col
            ? new ColumnRegion(_indexStart, _indexEnd)
            : new RowRegion(_indexStart, _indexEnd);
        sheet.MarkDirty(dirtyRegion);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.GetRowColStore(_axis).Restore(_restoreData);
        IRegion dirtyRegion = _axis == Axis.Col
            ? new ColumnRegion(_indexStart, _indexEnd)
            : new RowRegion(_indexStart, _indexEnd);
        sheet.MarkDirty(dirtyRegion);
        return true;
    }
}