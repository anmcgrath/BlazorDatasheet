using BlazorDatasheet.Data;

namespace BlazorDatasheet.Commands;

public class SetRowHeightCommand : IUndoableCommand
{
    private int _rowIndex { get; }
    private double _height { get; }
    private double _oldHeight;

    public SetRowHeightCommand(int rowIndex, double height)
    {
        _rowIndex = rowIndex;
        _height = height;
    }

    public bool Execute(Sheet sheet)
    {
        _oldHeight = sheet.RowHeights.GetSize(_rowIndex);
        sheet.SetRowHeightImpl(_rowIndex, _height);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.SetRowHeightImpl(_rowIndex, _height);
        return true;
    }
}