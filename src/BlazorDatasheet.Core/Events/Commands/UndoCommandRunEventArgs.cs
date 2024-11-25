using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Events.Commands;

public class UndoCommandRunEventArgs
{
    public IUndoableCommand Command { get; }
    public Sheet Sheet { get; }
    public bool Result { get; }

    public UndoCommandRunEventArgs(IUndoableCommand command, Sheet sheet, bool result)
    {
        Command = command;
        Sheet = sheet;
        Result = result;
    }
}