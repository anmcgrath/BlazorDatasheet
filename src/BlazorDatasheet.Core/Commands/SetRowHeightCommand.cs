using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands;

public class SetRowHeightCommand : IUndoableCommand
{
    private int RowStart { get; }
    public int RowEnd { get; }
    private double _height { get; }
    private RowInfoStoreRestoreData _restoreData;

    public SetRowHeightCommand(int rowStart, int rowEnd, double height)
    {
        RowStart = rowStart;
        RowEnd = rowEnd;
        _height = height;
    }

    public bool Execute(Sheet sheet)
    {
        _restoreData = sheet.Rows.SetRowHeightsImpl(RowStart, RowEnd, _height);
        sheet.MarkDirty(new RowRegion(RowStart, RowEnd));
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Rows.Restore(_restoreData);
        sheet.MarkDirty(new RowRegion(RowStart, RowEnd));
        return true;
    }
}