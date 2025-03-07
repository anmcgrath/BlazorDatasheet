using System.Collections;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Data;

internal class SheetRowEnumerator : MultiSparseSourceIterator<IReadOnlyCell>, IEnumerator<IReadOnlyCell>
{
    public SheetRowEnumerator(int rowIndex, Sheet sheet) : base(
        [
            new ColSparseSourceWrapper(sheet.Cells.GetFormulaStore(), rowIndex),
            new ColSparseSourceWrapper(sheet.Cells.GetCellDataStore(), rowIndex),
            new ColSparseSourceWrapper(sheet.Cells.GetFormatStore(), rowIndex),
            new ColSparseSourceWrapper(sheet.Cells.GetTypeStore(), rowIndex)
        ],
        sheet.NumCols,
        col => sheet.Cells.GetCell(rowIndex, col))
    {
    }
}

internal class ColSparseSourceWrapper : ISparseSource
{
    private IRowSource _rowSource;
    private int _rowIndex;

    public ColSparseSourceWrapper(IRowSource rowSource, int rowIndex)
    {
        _rowSource = rowSource;
        _rowIndex = rowIndex;
    }

    public int GetNextNonEmptyIndex(int index) => _rowSource.GetNextNonEmptyIndexInRow(_rowIndex, index);
}