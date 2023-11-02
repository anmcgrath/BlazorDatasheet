using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Commands;

public class SetColumnHeadingsCommand: IUndoableCommand
{
    private readonly int _colStart;
    private readonly int _colEnd;
    private readonly string _heading;
    private List<(int start, int end, string heading)> _restoreData;

    public SetColumnHeadingsCommand(int colStart, int colEnd, string heading)
    {
        _colStart = colStart;
        _colEnd = colEnd;
        _heading = heading;
    }

    public bool Execute(Sheet sheet)
    {
        _restoreData = sheet.Columns.SetColumnHeadingsImpl(_colStart, _colEnd, _heading);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        foreach (var heading in _restoreData)
        {
            sheet.Columns.SetColumnHeadingsImpl(heading.start, heading.end, heading.heading);
        }

        return true;
    }
}