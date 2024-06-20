using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Commands.RowCols;

public class SetRowHeadingsCommand : IUndoableCommand
{
    private readonly int _rowStart;
    private readonly int _rowEnd;
    private readonly string _heading;
    private List<(int start, int end, string heading)> _restoreData;

    public SetRowHeadingsCommand(int rowStart, int rowEnd, string heading)
    {
        _rowStart = rowStart;
        _rowEnd = rowEnd;
        _heading = heading;
    }

    public bool Execute(Sheet sheet)
    {
        _restoreData = sheet.Rows.SetRowHeadingsImpl(_rowStart, _rowEnd, _heading);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        foreach (var heading in _restoreData)
        {
            sheet.Rows.SetRowHeadingsImpl(heading.start, heading.end, heading.heading);
        }

        return true;
    }
}