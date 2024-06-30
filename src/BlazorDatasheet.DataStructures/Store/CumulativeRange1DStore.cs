using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Search;
using BlazorDatasheet.DataStructures.Util;

namespace BlazorDatasheet.DataStructures.Store;

public class CumulativeRange1DStore : Range1DStore<double>
{
    public readonly double Default;

    private readonly List<int> _storedPositionStarts = new();
    private readonly List<int> _storedPositionEnds = new();
    private readonly List<double> _cumulativeValuesAtEnd = new();
    private readonly List<double> _cumulativeValuesAtStart = new();

    /// <summary>
    /// Stores/retrieves widths of ranges. Useful for setting column/row size, because
    /// we can calculate the x/y positions of a row/column as well as distances between rows/columns.
    /// Each range index has a particular size with the cumulative being the position at the START of the range
    /// E.g say we set the size of 0 to 20 and the size of [2,3] to 30
    /// We then have the following
    ///    0        1        2    3      4
    /// | 20 | | default |   30  30  |  default
    /// The cumulative of 0 is always zero, the cumulative of 1 is 20, the cumulative of 2 is 20 + default, for 3 = 20 + default + 30.
    /// </summary>
    /// <param name="default">The size of a range if it has not been explicitly set.</param>
    public CumulativeRange1DStore(double @default) : base(@default)
    {
        Default = @default;
    }

    /// <summary>
    /// Sets the interval data, overriding any existing.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public override MergeableIntervalStoreRestoreData<OverwritingValue<double>> Set(int start, int end, double value)
    {
        var restoreData = base.Set(start, end, value);
        // update cumulative positions1
        UpdateCumulativePositionsFrom(start);
        return restoreData;
    }

    /// <summary>
    /// Returns the size of the interval [position, position]
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public double GetSize(int position)
    {
        return this.Get(position);
    }

    private void UpdateCumulativePositionsFrom(int position)
    {
        if (_storedPositionStarts.Any())
        {
            var index = _storedPositionStarts.BinarySearchIndexOf(position);
            // if it exists, clear from that position otherwise we want to clear from before it.
            if (index < 0)
                index = ~index - 1;
            var newPosition = _storedPositionStarts[Math.Max(0, index)];
            if (newPosition < position)
                position = newPosition;

            ClearCumulativeData(index);
        }

        var intervals = Intervals.GetIntervals(position, Intervals.End);
        foreach (var interval in intervals)
        {
            var existingCumEnd = _cumulativeValuesAtEnd.Any();
            var cumEndPrev = _cumulativeValuesAtEnd.LastOrDefault();
            var posEndPrev = _storedPositionEnds.LastOrDefault();
            var intervalSize = interval.Length * interval.Data.Value;
            //     e s
            // [   ] [   ] intervals are NON overlapping.
            // The end of one interval will never be equal to the start of another.
            // But the cum end of one interval is equal to cum start of another.
            // the only time we'd have an overlapping end position if if there isn't anything stored
            // and we our new interval starts at 0, because poSendPrev will be the default value of _storedPositionEnds (0)

            double newCumStart;
            if (!existingCumEnd)
                newCumStart = interval.Start * Default;
            else
                newCumStart = cumEndPrev + (interval.Start - posEndPrev - 1) * Default;
            _storedPositionStarts.Add(interval.Start);
            _storedPositionEnds.Add(interval.End);
            _cumulativeValuesAtStart.Add(newCumStart);
            _cumulativeValuesAtEnd.Add(newCumStart + intervalSize);
        }
    }

    private void ClearCumulativeData(int fromIndex)
    {
        if (fromIndex < 0)
            fromIndex = 0;
        var n = _storedPositionEnds.Count - fromIndex;
        _storedPositionEnds.RemoveRange(fromIndex, n);
        _storedPositionStarts.RemoveRange(fromIndex, n);
        _cumulativeValuesAtEnd.RemoveRange(fromIndex, n);
        _cumulativeValuesAtStart.RemoveRange(fromIndex, n);
    }

    /// <summary>
    /// Removes the intervals between and including <paramref name="start"/> amd <paramref name="end"/>
    /// and shifts the remaining values to the left.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public override MergeableIntervalStoreRestoreData<OverwritingValue<double>> Delete(int start, int end)
    {
        var res = base.Delete(start, end);
        UpdateCumulativePositionsFrom(start);
        return res;
    }

    /// <summary>
    /// Inserts n empty intervals at the position start.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="n"></param>
    public override MergeableIntervalStoreRestoreData<OverwritingValue<double>> InsertAt(int start, int n)
    {
        var restoreData = base.InsertAt(start, n);
        UpdateCumulativePositionsFrom(0);
        return restoreData;
    }

    /// <summary>
    /// Returns the cumulative range size at the START of the range index.
    /// For the first range (0), it is always zero.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public double GetCumulative(int position)
    {
        if (!Intervals.Any())
            return position * Default;

        if (position > Intervals.End)
            return _cumulativeValuesAtEnd.Last() + (position - _storedPositionEnds.Last() - 1) * Default;

        var indexStart = _storedPositionStarts.BinarySearchIndexOf(position);
        if (indexStart >= 0)
            return _cumulativeValuesAtStart[indexStart];

        // if inside an overlapping interval, calculate the cumulative by seeing how far from the start it is
        // and the cell size in the interval
        var overlapping = this.Intervals.GetIntervals(position, position).FirstOrDefault();
        if (overlapping != null)
        {
            var startPosnIndex = _storedPositionStarts.BinarySearchIndexOf(overlapping.Start);
            return _cumulativeValuesAtStart[startPosnIndex] + (position - overlapping.Start) * overlapping.Data.Value;
        }

        // otherwise we are to the right of zero, one or more intervals
        var closestRightStartPosition = ~indexStart;
        // we have already checked whether we are greater then the end, so this index must exist
        // plus we know we aren't overlapping any intervals.
        return _cumulativeValuesAtStart[closestRightStartPosition] -
               (_storedPositionStarts[closestRightStartPosition] - position) * Default;
    }

    /// <summary>
    /// Returns the size between the start of <paramref name="start"/> and <paramref name="end"/>
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public double GetSizeBetween(int start, int end)
    {
        var c0 = GetCumulative(start);
        var c1 = GetCumulative(end);
        return c1 - c0;
    }

    /// <summary>
    /// Returns the range position just BEFORE the cumulative position given.
    /// </summary>
    /// <param name="cumulative"></param>
    /// <returns></returns>
    public int GetPosition(double cumulative)
    {
        if (!_cumulativeValuesAtStart.Any())
            return (int)(cumulative / Default);

        //.....clast_start         clast_end      cum
        // .... [                       ]          x
        if (cumulative >= _cumulativeValuesAtEnd.Last())
            return _storedPositionEnds.Last() + 1 + (int)((cumulative - _cumulativeValuesAtEnd.Last()) / Default);

        var searchIndexStart = _cumulativeValuesAtStart.BinarySearchIndexOf(cumulative);
        if (searchIndexStart >= 0)
            return _storedPositionStarts[searchIndexStart];
        var searchIndexEnd = _cumulativeValuesAtEnd.BinarySearchIndexOf(cumulative);
        if (searchIndexEnd >= 0)
            return _storedPositionEnds[searchIndexEnd] + 1; // +1 because it goes into the next interval range

        searchIndexStart = ~searchIndexStart; // the next index after where it would have been found

        // if searchIndexStart = 0 then it is before the first interval and so can be calculated
        // by considering the offset from 0 and using default size
        if (searchIndexStart == 0)
            return (int)(cumulative / Default);

        if (cumulative > _cumulativeValuesAtEnd[searchIndexStart - 1])
        {
            // it is between ranges
            //          c-1end   cumulative   cstart
            // [       ],            x        [      ]
            var offset = cumulative - _cumulativeValuesAtEnd[searchIndexStart - 1];
            return _storedPositionEnds[searchIndexStart - 1] + (int)(offset / Default);
        }

        // otherwise it must be inside an interval
        // it is between ranges
        // c-1_start   cum    c-1end           cstart
        // [            x         ],           [                   ]

        // handle the case of c_1_start = c-1end
        if (Math.Abs(_cumulativeValuesAtStart[searchIndexStart - 1] - _cumulativeValuesAtEnd[searchIndexStart - 1]) <
            0.0001)
            return _storedPositionStarts[searchIndexStart - 1];

        var oi = Intervals.Get(_storedPositionStarts[searchIndexStart - 1]);
        return _storedPositionStarts[searchIndexStart - 1] +
               (int)((cumulative - _cumulativeValuesAtStart[searchIndexStart - 1]) / oi.Value);
    }

    public new void Restore(MergeableIntervalStoreRestoreData<OverwritingValue<double>> restoreData)
    {
        base.Restore(restoreData);
        UpdateCumulativePositionsFrom(0);
    }
}