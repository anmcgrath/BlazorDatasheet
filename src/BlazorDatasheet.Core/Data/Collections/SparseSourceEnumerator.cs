using System.Collections;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Data.Collections;

internal class SparseSourceEnumerator<T> : IEnumerator<T>
{
    private int _currentIndex = -1;

    internal SparseSourceEnumerator(IEnumerable<ISparseSource> sources, int maxLength, Func<int, T> itemFactory)
    {
        _sources = sources;
        _maxLength = maxLength;
        _itemFactory = itemFactory;
    }

    private IEnumerable<ISparseSource> _sources;
    private readonly int _maxLength;
    private readonly Func<int, T> _itemFactory;

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

        if (nextIndex == _currentIndex)
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

    public T Current => _itemFactory(_currentIndex);

    object? IEnumerator.Current => Current;

    public void Dispose()
    {
    }
}