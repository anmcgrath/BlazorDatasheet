using Microsoft.AspNetCore.Components.Web;

namespace BlazorDatasheet.Services;

/// <summary>
/// A window keyboard event with the extra information the datasheet needs from the browser.
/// </summary>
public class SheetKeyboardEventArgs : KeyboardEventArgs
{
    /// <summary>
    /// Whether the key was produced with the AltGraph modifier. Browsers commonly expose AltGraph
    /// as Ctrl+Alt as well, even though it is being used to type a character rather than a shortcut.
    /// </summary>
    public bool IsAltGraph { get; set; }

    /// <summary>
    /// Whether the event targeted an element that natively accepts text input (the cell editor).
    /// When false the browser will not insert the character anywhere, so the datasheet has to
    /// account for it itself.
    /// </summary>
    public bool IsEditableTarget { get; set; }
}
