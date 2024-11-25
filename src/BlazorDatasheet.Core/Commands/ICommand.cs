using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Commands;

/// <summary>
/// A command that can be executed on the sheet
/// </summary>
public interface ICommand
{
    public bool Execute(Sheet sheet);
    public bool CanExecute(Sheet sheet);
}