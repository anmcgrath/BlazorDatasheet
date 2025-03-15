using System.Collections;
using BlazorDatasheet.Core.Interfaces;

namespace BlazorDatasheet.Core.Data.Collections;

public class NonEmptyCellCollection : IEnumerable<IReadOnlyCell>
{
    private readonly int _rowIndex;
    private readonly Sheet _sheet;

    public NonEmptyCellCollection(int rowIndex, Sheet sheet)
    {
        _rowIndex = rowIndex;
        _sheet = sheet;
    }

    public IEnumerator<IReadOnlyCell> GetEnumerator()
    {
        return new SparseRowEnumerator(_rowIndex, _sheet);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}