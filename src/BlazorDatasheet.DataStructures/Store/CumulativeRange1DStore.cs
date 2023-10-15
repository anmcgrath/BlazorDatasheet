using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Search;
using BlazorDatasheet.DataStructures.Util;

namespace BlazorDatasheet.DataStructures.Store;

public class CumulativeRange1DStore : Range1DStore<double>
{
    public readonly double Default;

    private readonly List<int> _storedPositionStarts;
    private readonly List<int> _storedPositionEnds;
    private readonly List<double> _cumulativeValuesAtEnd;
    private readonly List<double> _cumulativeValuesAtStart;

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
        _storedPositionStarts = new() { 0 };
        _storedPositionEnds = new() { 0 };
        _cumulativeValuesAtStart = new() { 0 };
        _cumulativeValuesAtEnd = new() { Default };
    }

    public override List<(int stat, int end, double value)> Set(int start, int end, double value)
    {
        var res = base.Set(start, end, value);
        // update cumulative positions1
        UpdateCumulativePositons(start);
        return res;
    }

    public double GetSize(int position)
    {
        return this.Get(position);
    }

    private void UpdateCumulativePositons(int fromPosition)
    {
        var startIndex = Math.Max(0, _storedPositionStarts.BinarySearchClosest(fromPosition) - 1);
        if (startIndex > _storedPositionStarts.Count - 1)
            return;

        var currentEndPosition = _storedPositionEnds[startIndex];
        // remove existing because we'll be updating
        var r0 = SheetMath.ClampInt(0, _storedPositionStarts.Count, startIndex + 1);
        var n = _storedPositionStarts.Count - r0;
        _storedPositionStarts.RemoveRange(r0, n);
        _storedPositionEnds.RemoveRange(r0, n);
        _cumulativeValuesAtEnd.RemoveRange(r0, n);
        _cumulativeValuesAtStart.RemoveRange(r0, n);

        var intervals = _intervals.GetOverlappingIntervals(currentEndPosition + 1, _intervals.End);
        foreach (var interval in intervals)
        {
            var r_start = _storedPositionStarts.Last();
            var r_end = _storedPositionEnds.Last();
            var c_start = _cumulativeValuesAtStart.Last();
            var c_end = _cumulativeValuesAtEnd.Last();
            // r_start       r_end               i.start     i.end
            // [       size     ]     default       [          ]
            _cumulativeValuesAtStart.Add(c_end + (interval.Start - r_end - 1) * Default);
            _cumulativeValuesAtEnd.Add(_cumulativeValuesAtStart.Last() +
                                       interval.Length * interval.Data.Value);

            _storedPositionStarts.Add(interval.Start);
            _storedPositionEnds.Add(interval.End);
        }
    }

    public override List<(int start, int end, double value)> Cut(int start, int end)
    {
        var res = base.Cut(start, end);
        UpdateCumulativePositons(start);
        return res;
    }

    public override void InsertAt(int start, int n)
    {
        base.InsertAt(start, n);
        UpdateCumulativePositons(start);
    }

    /// <summary>
    /// Returns the cumulative range size at the START of the range index.
    /// For the first range (0), it is always zero.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public double GetCumulative(int position)
    {
        if (!_intervals.Any())
            return position * Default;

        if (position > _intervals.End)
            return _cumulativeValuesAtEnd.Last() + (position - _storedPositionEnds.Last() - 1) * Default;

        var indexStart = _storedPositionStarts.BinarySearchIndexOf(position);
        if (indexStart >= 0)
            return _cumulativeValuesAtStart[indexStart];

        // if inside an overlapping interval, calculate the cumulative by looking at the end posn
        // and the cell size in the interval
        var overlapping = this._intervals.GetOverlappingIntervals(position, position).FirstOrDefault();
        if (overlapping != null)
        {
            var indexEnd = _storedPositionEnds.BinarySearchIndexOf(overlapping.End);
            return _cumulativeValuesAtEnd[indexEnd] - (overlapping.End - position) * overlapping.Data.Value;
        }

        // otherwise we are to the right of zero, one or more intervals
        var closestRightStartPosition = ~indexStart;
        // we have already checked whether we are greater then the end, so this index must exist
        // plus we know we aren't overlapping any intervals.
        return _cumulativeValuesAtStart[closestRightStartPosition] -
               (_storedPositionStarts[closestRightStartPosition] - position) * Default;
    }

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
        if (!_cumulativeValuesAtStart.Any() || _cumulativeValuesAtStart.Count == 1)
            return (int)(cumulative / Default);

        //.....clast_start         clast_end      cum
        // .... [                       ]          x
        if (cumulative > _cumulativeValuesAtEnd.Last())
            return _storedPositionEnds.Last() + 1 + (int)((cumulative - _cumulativeValuesAtEnd.Last()) / Default);

        var searchIndexStart = _cumulativeValuesAtStart.BinarySearchIndexOf(cumulative);
        if (searchIndexStart >= 0)
            return _storedPositionStarts[searchIndexStart];
        var searchIndexEnd = _cumulativeValuesAtEnd.BinarySearchIndexOf(cumulative);
        if (searchIndexEnd >= 0)
            return _storedPositionEnds[searchIndexEnd] + 1; // +1 because it goes into the next interval range

        searchIndexStart = ~searchIndexStart; // the next index after where it would have been found

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
        if (_cumulativeValuesAtStart[searchIndexStart - 1] == _cumulativeValuesAtEnd[searchIndexStart - 1])
            return _storedPositionStarts[searchIndexStart - 1];

        var oi = _intervals.Get(_storedPositionStarts[searchIndexStart - 1]);
        return _storedPositionStarts[searchIndexStart - 1] +
               (int)((cumulative - _cumulativeValuesAtStart[searchIndexStart - 1]) / Default);
    }

    public override void BatchSet(List<(int start, int end, double data)> data)
    {
        if (!data.Any())
            return;
        base.BatchSet(data);
        this.UpdateCumulativePositons(data.Select(x => x.start).Min());
    }
}