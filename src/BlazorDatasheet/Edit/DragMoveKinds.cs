namespace BlazorDatasheet.Edit;

/// <summary>
/// What the user can move by dragging the selection: a range by its border, or whole rows or columns by their
/// headings (or border). Combine the flags to allow more than one.
/// </summary>
[Flags]
public enum DragMoveKinds
{
    None = 0,

    /// <summary>
    /// A range of cells that are not whole rows or columns, moved by its border.
    /// </summary>
    Ranges = 1,

    /// <summary>
    /// Whole rows, moved by their headings or border.
    /// </summary>
    Rows = 2,

    /// <summary>
    /// Whole columns, moved by their headings or border.
    /// </summary>
    Columns = 4,

    All = Ranges | Rows | Columns
}
