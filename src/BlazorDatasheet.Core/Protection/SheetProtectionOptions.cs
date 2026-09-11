namespace BlazorDatasheet.Core.Protection;

/// <summary>Operations allowed while a sheet is protected. All are denied by default.</summary>
public sealed record SheetProtectionOptions
{
    public bool AllowFormatCells { get; init; }
    public bool AllowFormatRows { get; init; }
    public bool AllowFormatColumns { get; init; }
    public bool AllowInsertRows { get; init; }
    public bool AllowInsertColumns { get; init; }
    public bool AllowDeleteRows { get; init; }
    public bool AllowDeleteColumns { get; init; }
    public bool AllowSort { get; init; }
    public bool AllowFilter { get; init; }

    /// <summary>
    /// Whether locked cells can be selected with the mouse or keyboard. Unlike every other option,
    /// this is allowed by default, matching spreadsheet convention.
    /// </summary>
    public bool AllowSelectLockedCells { get; init; } = true;
}

/// <summary>An operation governed by sheet protection.</summary>
public enum SheetOperation
{
    EditCells,
    FormatCells,
    FormatRows,
    FormatColumns,
    InsertRows,
    InsertColumns,
    DeleteRows,
    DeleteColumns,
    Sort,
    Filter,
    SelectLockedCells,
    Configure,
    Freeze
}
