using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Commands;

public class RemoveColumnCommand : IUndoableCommand
{
    private int _columnIndex;
    private readonly int _nCols;
    private List<CellChangedFormat> _removedCellFormats;
    private List<OrderedInterval<CellFormat>> _modifiedColumFormats;
    private List<DataRegion<bool>> _mergedRemoved;
    private List<DataRegion<int>> _validatorsRemoved;
    private int _nColsRemoved;
    private ClearCellsCommand _clearCellsCommand;
    private ColumnInfoRestoreData _columnInfoRestoreData;

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

        if (_nColsRemoved == 0)
            return false;

        ClearCells(sheet);
        RemoveFormats(sheet);
        RemoveColumnHeadingAndStoreWidth(sheet);

        _mergedRemoved = sheet.Merges.Store.RemoveCols(_columnIndex, _columnIndex + _nColsRemoved - 1);
        _validatorsRemoved = sheet.Validation.Store.RemoveCols(_columnIndex, _columnIndex + _nColsRemoved - 1);
        return sheet.RemoveColImpl(_columnIndex, _nColsRemoved);
    }

    private void RemoveColumnHeadingAndStoreWidth(Sheet sheet)
    {
        _columnInfoRestoreData = sheet.ColumnInfo.Cut(_columnIndex, _columnIndex + _nColsRemoved - 1);
    }

    private void RemoveFormats(Sheet sheet)
    {
        // Keep track of the values we have removed
        var nonEmptyCellPositions = sheet.GetNonEmptyCellPositions(new ColumnRegion(_columnIndex));
        _removedCellFormats = new List<CellChangedFormat>();
        

        foreach (var position in nonEmptyCellPositions)
        {
            var cell = sheet.GetCell(position.row, position.col);
            var value = cell.GetValue();
            var format = cell.Formatting;
            if (format != null)
                _removedCellFormats.Add(new CellChangedFormat(position.row, position.col, format, null));
        }

        _modifiedColumFormats = sheet.ColFormats.Remove(_columnIndex, _columnIndex + _nColsRemoved - 1);
        sheet.ColFormats.ShiftLeft(_columnIndex, _nColsRemoved);
    }

    private void ClearCells(Sheet sheet)
    {
        _clearCellsCommand =
            new ClearCellsCommand(sheet.Range(new ColumnRegion(_columnIndex, _columnIndex + _nColsRemoved - 1)));
        _clearCellsCommand.Execute(sheet);
    }

    public bool Undo(Sheet sheet)
    {
        // perform undos for merges, validation etc.
        UndoMerges(sheet);
        UndoValidation(sheet);

        // Insert column back in and set all the values that we removed
        sheet.InsertColAtImpl(_columnIndex, _nColsRemoved);
        
        sheet.ColumnInfo.Insert(_columnIndex, _nColsRemoved);
        sheet.ColumnInfo.RestoreFromData(_columnInfoRestoreData);

        // restore values
        _clearCellsCommand.Undo(sheet);

        sheet.ColFormats.ShiftRight(_columnIndex, _nColsRemoved);
        sheet.ColFormats.AddRange(_modifiedColumFormats);

        foreach (var changedFormat in _removedCellFormats)
        {
            sheet.SetCellFormat(changedFormat.Row, changedFormat.Col, changedFormat.OldFormat);
        }

        return true;
    }


    private void UndoValidation(Sheet sheet)
    {
        sheet.Validation.Store.InsertCols(_columnIndex, _nColsRemoved);
        foreach (var validator in _validatorsRemoved)
            sheet.Validation.Store.Add(validator.Region, validator.Data);
    }

    public void UndoMerges(Sheet sheet)
    {
        sheet.Merges.Store.InsertCols(_columnIndex, _nColsRemoved);
        foreach (var merge in _mergedRemoved)
            sheet.Merges.AddImpl(merge.Region);
    }
}