using System.Collections;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Data;

public class MultiSparseSourceIterator : IEnumerator<SheetRow>
{
    private int _currentIndex = -1;

    internal MultiSparseSourceIterator(IEnumerable<ISparseSource> sources, int maxLength)
    {
        _sources = sources;
        _maxLength = maxLength;
    }

    private IEnumerable<ISparseSource> _sources;
    private readonly int _maxLength;

    public bool MoveNext()
    {
        int nextIndex = int.MaxValue;
        foreach (var source in _sources)
        {
            var sourceNextIndex = source.GetNextNonEmptyIndex(_currentIndex);
            if (sourceNextIndex == -1)
                continue;
            if (sourceNextIndex == _currentIndex + 1)
            {
                nextIndex = sourceNextIndex;
                break;
            }

            nextIndex = Math.Min(sourceNextIndex, sourceNextIndex);
        }

        if (nextIndex == int.MaxValue)
            return false;

        _currentIndex = nextIndex;

        if (_currentIndex >= _maxLength)
            return false;

        return true;
    }

    public void Reset()
    {
        _currentIndex = -1;
    }

    public SheetRow Current => new SheetRow(_currentIndex);

    object? IEnumerator.Current => Current;

    public void Dispose()
    {
    }
}