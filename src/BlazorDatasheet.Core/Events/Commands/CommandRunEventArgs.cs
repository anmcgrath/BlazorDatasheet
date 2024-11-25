using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Events.Commands;

public class CommandRunEventArgs
{
    public ICommand Command { get; }
    public Sheet Sheet { get; }

    public bool Result { get; }

    public CommandRunEventArgs(ICommand command, Sheet sheet, bool result)
    {
        Command = command;
        Sheet = sheet;
        Result = result;
    }
}