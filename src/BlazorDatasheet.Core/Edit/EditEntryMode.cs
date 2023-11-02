namespace BlazorDatasheet.Core.Edit;

/// <summary>
/// The method used to begin/enter the edit.
/// </summary>
public enum EditEntryMode
{
    None,
    /// <summary>
    /// The user has used the mouse to enter.
    /// </summary>
    Mouse,
    /// <summary>
    /// The user has used the keyboard to enter.
    /// </summary>
    Key,
}