using BlazorDatasheet.Data;

namespace BlazorDatasheet.Commands;

public class CommandManager
{
    private Sheet _sheet;

    public CommandManager(Sheet sheet)
    {
        _sheet = sheet;
    }

    public void SetSheet(Sheet sheet)
    {
        _sheet = sheet;
    }
}