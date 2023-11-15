using BenchmarkDotNet.Attributes;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;

namespace Benchmarks.SparseMatrixStore;

public class GetNonEmptyRegions
{
    public int N { get; set; } = 1000;

    public Random r;
    private IMatrixDataStore<int> _store1 = new SparseMatrixStore<int>(-1);
    private IMatrixDataStore<int> _store2 = new SparseMatrixStore2<int>(-1);

    [GlobalSetup]
    public void Setup()
    {
        r = new Random(2);

        for (int i = 0; i < 200; i++)
        {
            var row = r.Next(1000);
            var col = r.Next(1000);
            var v = r.Next(1000);
            _store1.Set(row, col, v);
            _store2.Set(row, col, v);
        }
    }

    [Benchmark]
    public void SparseMatrix1Store_Gets()
    {
        for (int i = 0; i < N; i++)
        {
            var row = r.Next(1000);
            var col = r.Next(1000);
            _store1.GetNonEmptyData(new Region(row, row + 50, col, col + 50));
        }
    }
    
    [Benchmark]
    public void SparseMatrix2Store_Gets()
    {
        for (int i = 0; i < N; i++)
        {
            var row = r.Next(1000);
            var col = r.Next(1000);
            _store2.GetNonEmptyData(new Region(row, row + 50, col, col + 50));
        }
    }
}