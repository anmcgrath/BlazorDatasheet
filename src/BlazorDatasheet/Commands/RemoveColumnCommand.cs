using BlazorDatasheet.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.RTree;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.DataStructures.Util;
using BlazorDatasheet.Events;
using BlazorDatasheet.Formats;

namespace BlazorDatasheet.Commands;

public class RemoveColumnCommand : IUndoableCommand
{
    private int _columnIndex;
    private readonly int _nCols;
    private Heading _removedHeading;
    private double _removedWidth;
    private List<CellChangedFormat> _removedCellFormats;
    private OrderedInterval<CellFormat>? _modifiedColumFormat;
    private List<DataRegion<bool>> _mergedRemoved;
    private int _nColsRemoved;
    private ClearCellsCommand _clearCellsCommand;

    /// <summary>
    /// Command for removing a column at the index given.
    /// </summary>
    /// <param name="columnIndex">The column to remove.</param>
    public RemoveColumnCommand(int columnIndex, int nCols)
    {
        _columnIndex = columnIndex;
        _nCols = nCols;
    }

    public bool Execute(Sheet sheet)
    {
        if (_columnIndex >= sheet.NumCols)
            return false;
        if (_nCols <= 0)
            return false;
        _nColsRemoved = Math.Min(sheet.NumCols - _columnIndex + 1, _nCols);

        ClearCells(sheet);
        RemoveFormats(sheet);
        RemoveColumnHeadingAndStoreWidth(sheet);
        HandleRemoveAndContractMerges(sheet);
        return sheet.RemoveColImpl(_columnIndex, _nColsRemoved);
    }

    private void RemoveColumnHeadingAndStoreWidth(Sheet sheet)
    {
        _removedWidth = sheet.LayoutProvider.ComputeWidth(_columnIndex, 1);

        if (sheet.ColumnHeadings.Any() &&
            _columnIndex >= 0 &&
            _columnIndex < sheet.ColumnHeadings.Count)
        {
            _removedHeading = sheet.ColumnHeadings[_columnIndex];
        }
    }

    private void RemoveFormats(Sheet sheet)
    {
        // Keep track of the values we have removed
        var nonEmptyCellPositions = sheet.GetNonEmptyCellPositions(new ColumnRegion(_columnIndex));
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
            var format = cell.Formatting;
            if (format != null)
                _removedCellFormats.Add(new CellChangedFormat(position.row, position.col, format, null));
        }
    }

    private void ClearCells(Sheet sheet)
    {
        _clearCellsCommand =
            new ClearCellsCommand(sheet.Range(new ColumnRegion(_columnIndex, _columnIndex + _nColsRemoved - 1)));
        _clearCellsCommand.Execute(sheet);
    }

    private void HandleRemoveAndContractMerges(Sheet sheet)
    {
        _mergedRemoved = sheet.Merges.Store.RemoveCols(_columnIndex, _columnIndex + _nColsRemoved - 1);
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Merges.Store.InsertCols(_columnIndex, _nColsRemoved);
        foreach (var merge in _mergedRemoved)
            sheet.Merges.AddImpl(merge.Region);

        // Insert column back in and set all the values that we removed
        sheet.InsertColAtImpl(_columnIndex, _removedWidth, _nColsRemoved);

        _clearCellsCommand.Undo(sheet);

        if (_columnIndex >= 0 && _columnIndex < sheet.ColumnHeadings.Count)
            sheet.ColumnHeadings[_columnIndex] = _removedHeading;

        if (_modifiedColumFormat != null)
            sheet.ColFormats.Add(_modifiedColumFormat);

        foreach (var changedFormat in _removedCellFormats)
        {
            sheet.SetCellFormat(changedFormat.Row, changedFormat.Col, changedFormat.OldFormat);
        }

        return true;
    }
}