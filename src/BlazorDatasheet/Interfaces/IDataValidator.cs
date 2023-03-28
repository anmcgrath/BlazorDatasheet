namespace BlazorDatasheet.Interfaces;

public interface IDataValidator
{
    /// <summary>
    /// Returns whether a value passes the validation
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool IsValid(object? value);

    /// <summary>
    /// If IsStrict, the cell's value won't be changed if the validation fails during editing.
    /// If IsStrict=false, the cell value will be changed, but the cell will be marked as invalid.
    /// Note that setting a cell value programatically doesn't check IsStrict.
    /// </summary>
    public bool IsStrict { get; }
}