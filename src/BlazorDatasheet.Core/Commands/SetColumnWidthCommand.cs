using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Commands;

public class SetColumnWidthCommand : IUndoableCommand
{
    private readonly int _colStart;
    private readonly int _colEnd;
    private readonly double _width;
    private List<(int start, int end, double width)> _oldWidths;

    /// <summary>
    /// Command that changes the column width to the specified amount
    /// </summary>
    /// <param name="colStart">The index of the column to change.</param>
    /// <param name="colEnd">The end of the range to change</param>
    /// <param name="width">The new width of the column, in pixels</param>
    public SetColumnWidthCommand(int colStart, int colEnd, double width)
    {
        _colStart = colStart;
        _colEnd = colEnd;
        _width = width;
    }

    public bool Execute(Sheet sheet)
    {
        _oldWidths = sheet.ColumnInfo.SetColumnWidths(_colStart, _colEnd, _width);
        sheet.EmitColumnWidthChange(_colStart, _colEnd, _width);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        for (int i = 0; i < _oldWidths.Count; i++)
        {
            sheet.ColumnInfo.SetColumnWidths(_oldWidths[i].start, _oldWidths[i].end, _oldWidths[i].width);
            sheet.EmitColumnWidthChange(_oldWidths[i].start, _oldWidths[i].end, _oldWidths[i].width);
        }

        return true;
    }
}