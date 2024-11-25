using System.ComponentModel;
using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Events.Commands;

public class BeforeCommandRunEventArgs : CancelEventArgs
{
    public ICommand Command { get; }
    public Sheet Sheet { get; }

    public BeforeCommandRunEventArgs(ICommand command, Sheet sheet)
    {
        Command = command;
        Sheet = sheet;
    }
}