using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Commands;

public class SetRowHeightCommand : IUndoableCommand
{
    private int RowStart { get; }
    public int RowEnd { get; }
    private double _height { get; }
    private List<(int start, int end, double height)> _oldHeights;

    public SetRowHeightCommand(int rowStart, int rowEnd, double height)
    {
        RowStart = rowStart;
        RowEnd = rowEnd;
        _height = height;
    }

    public bool Execute(Sheet sheet)
    {
        _oldHeights = sheet.Rows.SetRowHeightsImpl(RowStart, RowEnd, _height);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        foreach (var old in _oldHeights)
        {
            sheet.Rows.SetRowHeightsImpl(old.start, old.end, old.height);
        }
        return true;
    }
}