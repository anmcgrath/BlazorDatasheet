using BenchmarkDotNet.Attributes;
using BlazorDatasheet.DataStructures.Store;

namespace Benchmarks.SparseMatrixStore;

public class SetRandomRowCols
{
    [Params(10, 100, 1000)]
    public int N { get; set; }

    public Random r;
    private IMatrixDataStore<int> _store1 = new SparseMatrixStore<int>(-1);
    private IMatrixDataStore<int> _store2 = new SparseMatrixStore2<int>(-1);

    [GlobalSetup]
    public void Setup()
    {
        r = new Random(2);
    }

    [Benchmark]
    public void SparseMatrix1Store_Sets()
    {
        for (int i = 0; i < N; i++)
        {
            var row = r.Next(1000);
            var col = r.Next(1000);
            var v = r.Next(1000);
            _store1.Set(row, col, v);
        }
    }
    
    [Benchmark]
    public void SparseMatrix2Store_Sets()
    {
        for (int i = 0; i < N; i++)
        {
            var row = r.Next(1000);
            var col = r.Next(1000);
            var v = r.Next(1000);
            _store2.Set(row, col, v);
        }
    }
}