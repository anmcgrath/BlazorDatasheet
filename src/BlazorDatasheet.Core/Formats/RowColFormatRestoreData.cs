using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Formats;

public class RowColFormatRestoreData
{
    internal List<CellStoreRestoreData> CellFormatRestoreData { get; set; } = new();
    internal List<OrderedInterval<CellFormat>> IntervalsRemoved { get; set; } = new();
    internal List<OrderedInterval<CellFormat>> IntervalsAdded { get; set; } = new();
    
    internal void Merge(RowColFormatRestoreData other)
    {
        CellFormatRestoreData.AddRange(other.CellFormatRestoreData);
        IntervalsRemoved.AddRange(other.IntervalsRemoved);
        IntervalsAdded.AddRange(other.IntervalsAdded);
    }
}