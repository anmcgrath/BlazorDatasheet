using BenchmarkDotNet.Attributes;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Util;

namespace Benchmarks.RangeEval;

public class ReferenceEvaluator
{
    private string[] _ranges;

    [GlobalSetup]
    public void SetupRanges()
    {
        var ranges = new string[]
        {
            "$A$1:B233",
            "$C4:22",
            "A$1:2d",
            "A1:a2",
            "AA102:f2",
            "AAA102:22",
            "B3:",
            "2:$3"
        };
        _ranges = Enumerable.Repeat(ranges, 20).SelectMany(x => x)
            .ToArray();
    }

    [Benchmark]
    public void RefFinder1()
    {
        foreach (var str in _ranges)
        {
            var res = Region.FromString(str);
        }
    }

    [Benchmark]
    public void RefFinder2()
    {
        foreach (var str in _ranges)
        {
            var res = RangeText2.TryParseReference(str.AsSpan(), out var refs);
        }
    }
}