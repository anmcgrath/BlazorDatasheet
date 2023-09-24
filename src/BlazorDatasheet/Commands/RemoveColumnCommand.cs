using BlazorDatasheet.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Events;
using BlazorDatasheet.Formats;

namespace BlazorDatasheet.Commands;

public class RemoveColumnCommand : IUndoableCommand
{
    private int _columnIndex;
    private Heading _removedHeading;
    private double _removedWidth;
    private List<CellChange> _removedValues;
    private List<CellChangedFormat> _removedCellFormats;
    private OrderedInterval<CellFormat>? _modifiedColumFormat;
    private IReadOnlyList<CellMerge> _mergesPerformed = default!;
    private IReadOnlyList<CellMerge> _overridenMergedRegions = default!;

    /// <summary>
    /// Command for removing a column at the index given.
    /// </summary>
    /// <param name="columnIndex">The column to remove.</param>
    public RemoveColumnCommand(int columnIndex)
    {
        _columnIndex = columnIndex;
    }

    public bool Execute(Sheet sheet)
    {
        // Keep track of the values we have removed
        var nonEmptyCellPositions = sheet.GetNonEmptyCellPositions(new ColumnRegion(_columnIndex));
        _removedValues = new List<CellChange>();
        _removedCellFormats = new List<CellChangedFormat>();

        var existingColumnFormatInterval =
            sheet.ColFormats.GetOverlappingIntervals(new OrderedInterval(_columnIndex, _columnIndex)).FirstOrDefault();

        if (existingColumnFormatInterval != null)
        {
            _modifiedColumFormat = existingColumnFormatInterval.Copy();
            var formatToShrink = existingColumnFormatInterval.Copy();
            formatToShrink.End = formatToShrink.End - 1;
            sheet.ColFormats.Delete(existingColumnFormatInterval);
            sheet.ColFormats.Add(formatToShrink);
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

        _removedWidth = sheet.LayoutProvider.ComputeWidth(_columnIndex, 1);

        if (sheet.ColumnHeadings.Any() &&
            _columnIndex >= 0 &&
            _columnIndex < sheet.ColumnHeadings.Count)
        {
            _removedHeading = sheet.ColumnHeadings[_columnIndex];
        }

        var res = sheet.RemoveColImpl(_columnIndex);

        (_mergesPerformed, _overridenMergedRegions) = sheet.Merges.RerangeMergedCells(Axis.Col, _columnIndex, -1);
        return res;
    }

    public bool Undo(Sheet sheet)
    {
        // Insert column back in and set all the values that we removed
        sheet.InsertColAfterImpl(_columnIndex - 1, _removedWidth);
        if (_columnIndex >= 0 && _columnIndex < sheet.ColumnHeadings.Count)
            sheet.ColumnHeadings[_columnIndex] = _removedHeading;
        sheet.SetCellValuesImpl(_removedValues);

        if (_modifiedColumFormat != null)
            sheet.ColFormats.Add(_modifiedColumFormat);

        foreach (var changedFormat in _removedCellFormats)
        {
            sheet.SetCellFormat(changedFormat.Row, changedFormat.Col, changedFormat.OldFormat);
        }

        sheet.Merges.UndoRerangeMergedCells(_mergesPerformed, _overridenMergedRegions);
        return true;
    }
}