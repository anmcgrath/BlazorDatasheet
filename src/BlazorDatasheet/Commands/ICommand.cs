using BlazorDatasheet.Data;

namespace BlazorDatasheet.Commands;

/// <summary>
/// A command that can be executed on the sheet
/// </summary>
public interface ICommand
{
    public bool Execute(Sheet sheet);
}