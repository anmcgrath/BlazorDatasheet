using BenchmarkDotNet.Attributes;
using BlazorDatasheet.DataStructures.Util;

namespace Benchmarks.RangeEval;

public class RangeEvaluator
{
    private string[] _ranges;

    [GlobalSetup]
    public void SetupRanges()
    {
        var ranges = new string[]
        {
            "$A$1",
            "$C4",
            "A$1",
            "A1",
            "AA102",
            "AAA102",
            "B3"
        };
        _ranges = Enumerable.Repeat(ranges, 20).SelectMany(x => x)
            .ToArray();
    }

    [Benchmark]
    public void Range1()
    {
        foreach (var str in _ranges)
        {
            var r = RangeText.CellFromString(str);
        }
    }

    [Benchmark]
    public void Range2()
    {
        foreach (var str in _ranges)
        {
            var parsed = RangeText2.TryParseSingleCellReference(str, out var cellRange);
        }
    }
}