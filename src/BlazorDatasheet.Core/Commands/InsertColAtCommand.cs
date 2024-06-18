using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Commands;

/// <summary>
/// Command for inserting a column in to the sheet
/// </summary>
public class InsertColAtCommand : IUndoableCommand
{
    private readonly int _colIndex;
    private readonly int _nCols;

    private RegionRestoreData<int> _validatorRestoreData;
    private RegionRestoreData<ConditionalFormatAbstractBase> _cfRestoreData;
    private CellStoreRestoreData _cellStoreRestoreData;

    /// <summary>
    /// Command for inserting a column into the sheet.
    /// </summary>
    /// <param name="colIndex">The index that the column will be inserted at.</param>
    public InsertColAtCommand(int colIndex, int nCols = 1)
    {
        _colIndex = colIndex;
        _nCols = nCols;
    }

    public bool Execute(Sheet sheet)
    {
        _validatorRestoreData = sheet.Validators.Store.InsertCols(_colIndex, _nCols);
        _cellStoreRestoreData = sheet.Cells.InsertColAt(_colIndex, _nCols);
        _cfRestoreData = sheet.ConditionalFormats.InsertColAtImpl(_colIndex, _nCols);
        sheet.AddCols(_nCols);
        sheet.Columns.InsertImpl(_colIndex, _nCols);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Cells.Restore(_cellStoreRestoreData);
        sheet.Validators.Store.Restore(_validatorRestoreData);
        sheet.RemoveCols(_nCols);
        sheet.ConditionalFormats.Restore(_cfRestoreData);
        sheet.Columns.RemoveColumnsImpl(_colIndex, _colIndex + _nCols - 1);
        return true;
    }
}