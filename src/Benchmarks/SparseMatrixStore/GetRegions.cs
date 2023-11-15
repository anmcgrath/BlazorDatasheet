using BenchmarkDotNet.Attributes;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;

namespace Benchmarks.SparseMatrixStore;

public class GetRegions
{

    public int N { get; set; } = 1000;
    
    [Params(1,5,10,20)]
    public int RowSize { get; set; }
    [Params(1,5,10,20)]
    public int ColSize { get; set; }

    public Random r;
    private IMatrixDataStore<int> _store1 = new SparseMatrixStore<int>(-1);
    private IMatrixDataStore<int> _store2 = new SparseMatrixStore2<int>(-1);

    [GlobalSetup]
    public void Setup()
    {
        r = new Random(2);

        for (int i = 0; i < 3000; i++)
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
            _store1.GetData(new Region(row, row + RowSize, col, col + ColSize));
        }
    }
    
    [Benchmark]
    public void SparseMatrix2Store_Gets()
    {
        for (int i = 0; i < N; i++)
        {
            var row = r.Next(1000);
            var col = r.Next(1000);
            _store2.GetData(new Region(row, row + RowSize, col, col + ColSize));
        }
    }
}