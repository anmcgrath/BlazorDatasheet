namespace BlazorDatasheet.Core.Serialization.Models;

internal class TableModel
{
    public string Name { get; set; } = string.Empty;
    public string Sheet { get; set; } = string.Empty;

    /// <summary>
    /// The table's region, or its last region if it is deleted. Null for a sheet table.
    /// </summary>
    public string? Region { get; set; }

    public bool IsSheetTable { get; set; }
    public bool ShowTotals { get; set; }
    public bool Deleted { get; set; }
}
