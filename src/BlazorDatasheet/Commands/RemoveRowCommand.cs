using BlazorDatasheet.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.RTree;
using BlazorDatasheet.Events;
using BlazorDatasheet.Formats;

namespace BlazorDatasheet.Commands;

public class RemoveRowCommand : IUndoableCommand
{
    private readonly int _rowIndex;
    private List<CellChange> _removedValues;
    private List<CellChangedFormat> _removedCellFormats;
    private OrderedInterval<CellFormat>? _modifiedRowFormat;
    private IReadOnlyList<CellMerge> _mergesPerformed = default!;
    private IReadOnlyList<CellMerge> _overridenMergedRegions = default!;
    private List<CellMerge> _mergedRemoved;

    /// <summary>
    /// Command to remove the row at the index given.
    /// </summary>
    /// <param name="rowIndex">The row to remove.</param>
    public RemoveRowCommand(int rowIndex)
    {
        _rowIndex = rowIndex;
    }

    public bool Execute(Sheet sheet)
    {
        // Keep track of the values we have removed
        var nonEmptyCellPositions = sheet.GetNonEmptyCellPositions(new RowRegion(_rowIndex));
        _removedValues = new List<CellChange>();
        _removedCellFormats = new List<CellChangedFormat>();

        var existingRowFormatInterval =
            sheet.RowFormats.GetOverlappingIntervals(new OrderedInterval(_rowIndex, _rowIndex)).FirstOrDefault();

        if (existingRowFormatInterval != null)
        {
            _modifiedRowFormat = existingRowFormatInterval.Copy();
            var formatToShrink = existingRowFormatInterval.Copy();
            formatToShrink.End = formatToShrink.End - 1;
            sheet.RowFormats.Delete(existingRowFormatInterval);
            sheet.RowFormats.Add(formatToShrink);
        }

        foreach (var position in nonEmptyCellPositions)
        {
            var cell = sheet.GetCell(position.row, position.col);
            var value = cell.GetValue();
            if (value != null)
                _removedValues.Add(new CellChange(position.row, position.col, cell.GetValue()));
            var format = cell.Formatting;
            if (format != null)
                _removedCellFormats.Add(new CellChangedFormat(position.row, position.col, format, null));
        }

        _mergedRemoved = sheet
                         .Merges.MergedCells
                         .Search(new Envelope(0, _rowIndex, sheet.NumCols, _rowIndex))
                         .Where(x => x.Region.Height == 1)
                         .ToList();


        foreach (var merge in _mergedRemoved)
        {
            sheet.Merges.UnMergeCellsImpl(merge.Region);
        }

        (_mergesPerformed, _overridenMergedRegions) = sheet.Merges.RerangeMergedCells(Axis.Row, _rowIndex, -1);
        return sheet.RemoveRowAtImpl(_rowIndex);
    }

    public bool Undo(Sheet sheet)
    {
        sheet.InsertRowAfterImpl(_rowIndex - 1);
        sheet.SetCellValuesImpl(_removedValues);

        if (_modifiedRowFormat != null)
            sheet.RowFormats.Add(_modifiedRowFormat);

        foreach (var changedFormat in _removedCellFormats)
        {
            sheet.SetCellFormat(changedFormat.Row, changedFormat.Col, changedFormat.OldFormat);
        }


        sheet.Merges.UndoRerangeMergedCells(_mergesPerformed, _overridenMergedRegions);

        foreach (var mergeRemoved in _mergedRemoved)
        {
            sheet.Merges.Add(mergeRemoved.Region);
        }

        return true;
    }
}