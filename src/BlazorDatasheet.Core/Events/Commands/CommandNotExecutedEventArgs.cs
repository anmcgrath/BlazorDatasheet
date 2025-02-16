using BlazorDatasheet.Core.Commands;

namespace BlazorDatasheet.Core.Events.Commands;

public class CommandNotExecutedEventArgs
{
    public ICommand Command { get; }

    public CommandNotExecutedEventArgs(ICommand command)
    {
        Command = command;
    }
}