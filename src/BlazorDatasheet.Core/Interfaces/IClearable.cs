namespace BlazorDatasheet.Core.Interfaces;

/// <summary>
/// If a cell's Data implements this, this function will be called when the cell is cleared
/// in order to perform custom logic for clearing the data.
/// </summary>
public interface IClearable
{
    public void Clear(string? key);
}