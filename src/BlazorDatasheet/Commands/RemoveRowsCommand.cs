using BlazorDatasheet.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;
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
    private List<OrderedInterval<CellFormat>> _modifiedRowFormats;
    private List<DataRegion<bool>> _mergedRemoved;

    private List<DataRegion<int>> _validatorsRemoved;

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
        _mergedRemoved = sheet.Merges.Store.RemoveRows(_rowIndex, _rowIndex + _nRowsRemoved - 1);
        _validatorsRemoved = sheet.Validation.Store.RemoveRows(_rowIndex, _rowIndex + _nRowsRemoved - 1);
        return sheet.RemoveRowAtImpl(_rowIndex, _nRowsRemoved);
    }

    private bool ClearCells(Sheet sheet)
    {
        // Keep track of the values we have removed
        _clearCellsCommand =
            new ClearCellsCommand(sheet.Range(new RowRegion(_rowIndex, _rowIndex + _nRowsRemoved - 1)));
        return _clearCellsCommand.Execute(sheet);
    }

    private void RemoveFormats(Sheet sheet)
    {
        var nonEmptyCellPositions =
            sheet.GetNonEmptyCellPositions(new RowRegion(_rowIndex, _rowIndex + _nRowsRemoved - 1));
        _removedCellFormats = new List<CellChangedFormat>();

        foreach (var position in nonEmptyCellPositions)
        {
            var cell = sheet.GetCell(position.row, position.col);
            var format = cell.Formatting;
            if (format != null)
                _removedCellFormats.Add(new CellChangedFormat(position.row, position.col, format, null));
        }

        _modifiedRowFormats = sheet.RowFormats.Remove(_rowIndex, _rowIndex + _nRowsRemoved - 1);
        sheet.RowFormats.ShiftLeft(_rowIndex, _nRowsRemoved);
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Merges.Store.InsertRows(_rowIndex, _nRowsRemoved);
        foreach (var merge in _mergedRemoved)
            sheet.Merges.AddImpl(merge.Region);

        sheet.Validation.Store.InsertRows(_rowIndex, _nRowsRemoved);
        foreach (var validator in _validatorsRemoved)
            sheet.Validation.Store.Add(validator.Region, validator.Data);

        sheet.InsertRowAtImpl(_rowIndex);

        _clearCellsCommand.Undo(sheet);

        sheet.RowFormats.ShiftRight(_rowIndex, _nRowsRemoved);
        sheet.RowFormats.AddRange(_modifiedRowFormats);

        foreach (var changedFormat in _removedCellFormats)
        {
            sheet.SetCellFormat(changedFormat.Row, changedFormat.Col, changedFormat.OldFormat);
        }

        return true;
    }
}