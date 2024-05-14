//benchmark sparselist

using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BlazorDatasheet.DataStructures.Store;

var benchmark = BenchmarkRunner.Run<SparseMatrixBenchmark>();

public class SparseMatrixBenchmark
{
    private IMatrixDataStore<string> _sparseMatrixStoreByRows;
    private IMatrixDataStore<string> _sparseMatrixStoreByRow2;
    private IMatrixDataStore<string> _sparseMatrixStoreByCol;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _sparseMatrixStoreByRows = new SparseMatrixStoreByRows<string>();
        _sparseMatrixStoreByCol = new SparseMatrixStoreByCols<string>();
        Setup(_sparseMatrixStoreByRows);
        Setup(_sparseMatrixStoreByRow2);
        Setup(_sparseMatrixStoreByCol);
    }

    private void Setup(IMatrixDataStore<string> store)
    {
        var r = new Random(1);
        for (var i = 0; i < 1000; i++)
        {
            store.Set(r.Next(0, 1000), r.Next(0, 1000), "A");
        }
    }

    [Benchmark]
    public void Get()
    {
        _sparseMatrixStoreByRows.Get(100, 100);
    }
    
    [Benchmark]
    public void Get2()
    {
        _sparseMatrixStoreByRow2.Get(100, 100);
    }
    
    [Benchmark]
    public void Get3()
    {
        _sparseMatrixStoreByCol.Get(100, 100);
    }
    
    [Benchmark]
    public void Set()
    {
        _sparseMatrixStoreByRows.Set(100, 100 , "B");
    }
    
    [Benchmark]
    public void Set2()
    {
        _sparseMatrixStoreByRow2.Set(100, 100, "B");
    }
    
    [Benchmark]
    public void Set3()
    {
        _sparseMatrixStoreByCol.Set(100, 100, "B");
    }
}