using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands;

public class HideRowsCommand : IUndoableCommand
{
    private int _start;
    private int _end;

    private RowInfoStoreRestoreData _restoreData = null!;

    public HideRowsCommand(int start, int end)
    {
        _start = start;
        _end = end;
    }

    public bool Execute(Sheet sheet)
    {
        _restoreData = sheet.Rows.HideRowsImpl(_start, _end - _start + 1);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Rows.Restore(_restoreData);
        sheet.MarkDirty(new RowRegion(_start, _end));
        return true;
    }
}