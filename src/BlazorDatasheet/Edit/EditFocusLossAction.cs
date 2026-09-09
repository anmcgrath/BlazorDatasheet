namespace BlazorDatasheet.Edit;

/// <summary>
/// What happens to an open cell edit when browser focus moves from the sheet to another element on
/// the page. Focus leaving the window or tab always keeps the edit open.
/// </summary>
public enum EditFocusLossAction
{
    /// <summary>The edit stays open and continues when focus returns.</summary>
    KeepEditing,

    /// <summary>The edit is accepted. If the value is invalid the edit stays open.</summary>
    Accept,

    /// <summary>The edit is cancelled and the cell keeps its previous value.</summary>
    Cancel
}
