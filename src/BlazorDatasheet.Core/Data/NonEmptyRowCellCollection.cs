using System.Collections;
using BlazorDatasheet.Core.Interfaces;

namespace BlazorDatasheet.Core.Data;

public class NonEmptyRowCellCollection : IEnumerable<IReadOnlyCell>
{
    private readonly int _rowIndex;
    private readonly Sheet _sheet;

    public NonEmptyRowCellCollection(int rowIndex, Sheet sheet)
    {
        _rowIndex = rowIndex;
        _sheet = sheet;
    }

    public IEnumerator<IReadOnlyCell> GetEnumerator()
    {
        return new SheetRowEnumerator(_rowIndex, _sheet);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}