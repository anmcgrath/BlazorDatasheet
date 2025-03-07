using System.Collections;

namespace BlazorDatasheet.Core.Data;

public class NonEmptyRowCollection : IEnumerable<SheetRow>
{
    private readonly RowInfoStore _store;

    public NonEmptyRowCollection(RowInfoStore store)
    {
        _store = store;
    }


    public IEnumerator<SheetRow> GetEnumerator()
    {
        return new MultiSparseSourceIterator<SheetRow>(
            [
                _store.Sheet.Cells.GetFormulaStore(),
                _store.Sheet.Cells.GetCellDataStore(),
                _store.Sheet.Cells.GetFormatStore(),
                _store.Sheet.Cells.GetTypeStore(),
                _store.SizeStore,
                _store.Formats,
                _store.HeadingStore
            ], _store.Sheet.NumRows, rowIndex => new SheetRow(rowIndex, _store.Sheet)
        );
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}