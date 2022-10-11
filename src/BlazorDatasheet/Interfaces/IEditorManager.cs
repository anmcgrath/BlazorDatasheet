namespace BlazorDatasheet.Interfaces;

public interface IEditorManager
{
    /// <summary>
    /// Gets the actively edited value, that is saved even if an editor is disposed
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T GetEditedValue<T>();

    /// <summary>
    /// Sets the actively edited value, that is saved even if an editor is disposed
    /// </summary>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    void SetEditedValue<T>(T value);

    /// <summary>
    /// Accepts the current edit & attempts to set the active cell's value based on the current edited value.
    /// Applies data validation when setting the value.
    /// </summary>
    /// <returns>Whether the accepted edit was successful</returns>
    bool AcceptEdit();

    /// <summary>
    /// Cancels the current edit (without setting the active cell's value).
    /// </summary>
    /// <returns>Whether the edit has been cancelled</returns>
    bool CancelEdit();
}