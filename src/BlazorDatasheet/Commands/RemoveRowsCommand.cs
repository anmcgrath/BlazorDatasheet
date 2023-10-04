using BlazorDatasheet.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.RTree;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.DataStructures.Util;
using BlazorDatasheet.Events;
using BlazorDatasheet.Formats;

namespace BlazorDatasheet.Commands;

public class RemoveRowsCommand : IUndoableCommand
{
    private readonly int _rowIndex;
    private readonly int _nRows;

    private List<CellChangedFormat> _removedCellFormats;
    private OrderedInterval<CellFormat>? _modifiedRowFormat;
    private List<DataRegion<bool>> _mergedRemoved;
    // The actual number of rows removed (takes into account num of rows in sheet)
    private int _nRowsRemoved;
    private ClearCellsCommand _clearCellsCommand;

    /// <summary>
    /// Command to remove the row at the index given.
    /// </summary>
    /// <param name="rowIndex">The row to remove.</param>
    /// <param name="nRows">The number of rows to remove</param>
    public RemoveRowsCommand(int rowIndex, int nRows = 1)
    {
        _rowIndex = rowIndex;
        _nRows = nRows;
    }

    public bool Execute(Sheet sheet)
    {
        if (_rowIndex >= sheet.NumRows)
            return false;
        if (_nRows <= 0)
            return false;
        _nRowsRemoved = Math.Min(sheet.NumRows - _rowIndex + 1, _nRows);

        ClearCells(sheet);
        RemoveFormats(sheet);
        HandleRemoveAndContractMerges(sheet);

        return sheet.RemoveRowAtImpl(_rowIndex, _nRowsRemoved);
    }

    private bool ClearCells(Sheet sheet)
    {
        // Keep track of the values we have removed
        _clearCellsCommand =
            new ClearCellsCommand(sheet.Range(new RowRegion(_rowIndex, _rowIndex + _nRowsRemoved - 1)));
        return _clearCellsCommand.Execute(sheet);
    }

    private void HandleRemoveAndContractMerges(Sheet sheet)
    {
        _mergedRemoved = sheet.Merges.Store.RemoveRows(_rowIndex, _rowIndex + _nRowsRemoved - 1);
    }

    private void RemoveFormats(Sheet sheet)
    {
        var nonEmptyCellPositions =
            sheet.GetNonEmptyCellPositions(new RowRegion(_rowIndex, _rowIndex + _nRowsRemoved - 1));
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
            var format = cell.Formatting;
            if (format != null)
                _removedCellFormats.Add(new CellChangedFormat(position.row, position.col, format, null));
        }
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Merges.Store.InsertRows(_rowIndex, _nRowsRemoved);
        foreach (var merge in _mergedRemoved)
            sheet.Merges.AddImpl(merge.Region);

        sheet.InsertRowAtImpl(_rowIndex);

        _clearCellsCommand.Undo(sheet);

        if (_modifiedRowFormat != null)
            sheet.RowFormats.Add(_modifiedRowFormat);

        foreach (var changedFormat in _removedCellFormats)
        {
            sheet.SetCellFormat(changedFormat.Row, changedFormat.Col, changedFormat.OldFormat);
        }

        return true;
    }
}