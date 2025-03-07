using System.Collections;

namespace BlazorDatasheet.Core.Data.Collections;

public class NonEmptyColumnCollection : IEnumerable<SheetColumn>
{
    private readonly RowInfoStore _store;

    public NonEmptyColumnCollection(RowInfoStore store)
    {
        _store = store;
    }

    public IEnumerator<SheetColumn> GetEnumerator()
    {
        return new SparseSourceEnumerator<SheetColumn>(
            [
                _store.SizeStore,
                _store.Formats,
                _store.HeadingStore
            ], _store.Sheet.NumCols, colIndex => new SheetColumn(colIndex, _store.Sheet)
        );
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}