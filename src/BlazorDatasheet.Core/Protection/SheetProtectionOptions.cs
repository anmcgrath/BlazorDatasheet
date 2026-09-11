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
    Configure,
    Freeze
}
