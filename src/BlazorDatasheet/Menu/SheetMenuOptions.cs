using BlazorDatasheet.Core.Data;
using Microsoft.AspNetCore.Components;

namespace BlazorDatasheet.Menu;

public class SheetMenuOptions
{
    internal RenderFragment<Sheet>? CustomMenuFragment { get; set; } 

    /// <summary>
    /// When true, the context menu (on right mouse click) is enabled
    /// </summary>
    public bool ContextMenuEnabled { get; set; } = true;

    /// <summary>
    /// When true, the menu icon in the column header is enabled
    /// </summary>
    public bool HeaderMenuEnabled { get; set; } = true;

    /// <summary>
    /// Enables the menu "Clear" option
    /// </summary>
    public bool ClearEnabled { get; set; } = true;

    /// <summary>
    /// Enables the menu "Merge" option
    /// </summary>
    public bool MergeEnabled { get; set; } = true;

    /// <summary>
    /// Enables the menu "Alignment" option
    /// </summary>
    public bool AlignmentEnabled { get; set; } = true;

    /// <summary>
    /// Enables the menu "Insert rows" option
    /// </summary>
    public bool InsertRowsEnabled { get; set; } = true;

    /// <summary>
    /// Enables the menu "Insert columns" option
    /// </summary>
    public bool InsertColsEnabled { get; set; } = true;

    /// <summary>
    /// Enables the menu "Delete rows" option
    /// </summary>
    public bool DeleteRowsEnabled { get; set; } = true;

    /// <summary>
    /// Enables the menu "Delete columns" option
    /// </summary>
    public bool DeleteColsEnabled { get; set; } = true;

    /// <summary>
    /// Enables "hide/un-hide rows"
    /// </summary>
    public bool HideRowsEnabled { get; set; } = true;

    /// <summary>
    /// Enables "hide/un-hide columns"
    /// </summary>
    public bool HideColsEnabled { get; set; } = true;

    /// <summary>
    /// Enables "sort" on range selections
    /// </summary>
    public bool SortRangeEnabled { get; set; } = true;

    /// <summary>
    /// Enables "Filter columns" on column selections
    /// </summary>
    public bool FilterColumnEnabled { get; set; } = true;

    internal bool CompareTo(SheetMenuOptions other)
    {
        return other.InsertColsEnabled == InsertColsEnabled &&
               other.AlignmentEnabled == AlignmentEnabled &&
               other.ClearEnabled == ClearEnabled &&
               other.MergeEnabled == MergeEnabled &&
               other.DeleteRowsEnabled == DeleteRowsEnabled &&
               other.DeleteColsEnabled == DeleteColsEnabled &&
               other.InsertRowsEnabled == InsertRowsEnabled &&
               other.HideColsEnabled == HideColsEnabled &&
               other.HideRowsEnabled == HideRowsEnabled &&
               other.FilterColumnEnabled == FilterColumnEnabled &&
               other.SortRangeEnabled == SortRangeEnabled &&
               other.ContextMenuEnabled == ContextMenuEnabled &&
               other.HeaderMenuEnabled == HeaderMenuEnabled &&
               other.CustomMenuFragment == CustomMenuFragment;
    }
}