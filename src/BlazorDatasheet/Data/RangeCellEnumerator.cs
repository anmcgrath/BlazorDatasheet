using System.Collections;

namespace BlazorDatasheet.Data;

public class RangeCellEnumerator : IEnumerable<Cell>
{
    public IEnumerator<Cell> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}