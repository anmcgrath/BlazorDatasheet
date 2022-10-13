using BlazorDatasheet.Data;

namespace BlazorDatasheet.Commands;

public interface ICommand
{
    public bool Execute(Sheet sheet);
}