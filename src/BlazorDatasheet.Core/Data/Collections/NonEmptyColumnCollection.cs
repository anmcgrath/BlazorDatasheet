using System.Collections;

namespace BlazorDatasheet.Core.Data.Collections;

public class NonEmptyColumnCollection : IEnumerable<SheetColumn>
{
    private readonly ColumnInfoStore _store;

    public NonEmptyColumnCollection(ColumnInfoStore store)
    {
        _store = store;
    }

    public IEnumerator<SheetColumn> GetEnumerator()
    {
        return new SparseSourceEnumerator<SheetColumn>(
            [
                _store.SizeStore,
                _store.Formats,
                _store.Visible,
                _store.HeadingStore,
                _store.Filters.Store,
            ], _store.Sheet.NumCols, colIndex => new SheetColumn(colIndex, _store.Sheet)
        );
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}