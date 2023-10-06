using BlazorDatasheet.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Commands;

/// <summary>
/// Command for inserting a column in to the sheet
/// </summary>
public class InsertColAtCommand : IUndoableCommand
{
    private readonly int _colIndex;
    private readonly int _nCols;

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
        sheet.Merges.Store.InsertCols(_colIndex, _nCols);
        sheet.Validation.Store.InsertCols(_colIndex, _nCols);
        sheet.InsertColAtImpl(_colIndex, null, _nCols);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Merges.Store.RemoveCols(_colIndex, _colIndex + _nCols - 1);
        sheet.Validation.Store.RemoveCols(_colIndex, _colIndex + _nCols - 1);
        sheet.RemoveColImpl(_colIndex, _nCols);
        return true;
    }
}