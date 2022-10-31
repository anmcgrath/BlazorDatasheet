using System.Linq;
using BlazorDatasheet.Data;
using NUnit.Framework;

namespace BlazorDatasheet.Test;

public class DataStoreTests
{
    [Test]
    public void Set_Get_Operations_Correct()
    {
        IMatrixDataStore<string> store = new SparseMatrixStore<string>();
        Assert.AreEqual(default(Cell), store.Get(0, 0));
        store.Set(0, 0, "A");
        Assert.AreEqual("A", store.Get(0, 0));
        store.Set(10, 10, "B");
        Assert.AreEqual("B", store.Get(10, 10));
        store.Set(10000000, 10000000, "C");
        Assert.AreEqual("C", store.Get(10000000, 10000000));
    }

    [Test]
    public void Insert_Row_After_Existing_Operations_Correct()
    {
        IMatrixDataStore<string> store = new SparseMatrixStore<string>();
        store.Set(0, 0, "A");
        store.Set(1, 0, "B");
        store.Set(2, 0, "C");
        store.Set(3, 0, "D");

        store.InsertRowAt(0);
        Assert.AreEqual("A", store.Get(0, 0));
        Assert.AreEqual(default(string), store.Get(1, 0));
        Assert.AreEqual("B", store.Get(2, 0));
        Assert.AreEqual("C", store.Get(3, 0));
        Assert.AreEqual("D", store.Get(4, 0));
        Assert.AreEqual(default(string), store.Get(5, 0));
    }

    [Test]
    public void Insert_Row_After_Non_Existing_Operations_Correct()
    {
        IMatrixDataStore<string> store = new SparseMatrixStore<string>();
        store.Set(0, 0, "A");
        store.Set(2, 0, "B");
        store.Set(3, 0, "C");

        store.InsertRowAt(1);
        Assert.AreEqual("A", store.Get(0, 0));
        Assert.AreEqual(default(string), store.Get(1, 0));
        Assert.AreEqual(default(string), store.Get(2, 0));
        Assert.AreEqual("B", store.Get(3, 0));
        Assert.AreEqual("C", store.Get(4, 0));
    }

    [Test]
    public void Remove_NonEmpty_Row_Operations_Correct()
    {
        IMatrixDataStore<string> store = new SparseMatrixStore<string>();
        store.Set(0, 0, "A");
        store.Set(1, 0, "B");
        store.Set(2, 0, "C");
        store.Set(3, 0, "D");

        store.RemoveRowAt(0);
        Assert.AreEqual("B", store.Get(0, 0));
        Assert.AreEqual("C", store.Get(1, 0));
        Assert.AreEqual("D", store.Get(2, 0));
        Assert.AreEqual(default(string), store.Get(3, 0));
    }

    [Test]
    public void Remove_Empty_Row_Operations_Correct()
    {
        IMatrixDataStore<string> store = new SparseMatrixStore<string>();
        store.Set(0, 0, "A");
        store.Set(2, 0, "B");
        store.Set(3, 0, "C");

        store.RemoveRowAt(1);
        Assert.AreEqual("A", store.Get(0, 0));
        Assert.AreEqual("B", store.Get(1, 0));
        Assert.AreEqual("C", store.Get(2, 0));
    }

    [Test]
    public void Get_Next_Non_Empty_Row_Num_Correct()
    {
        IMatrixDataStore<string> store = new SparseMatrixStore<string>();
        store.Set(5, 10, "A");
        store.Set(100, 10, "B");
        var nextRow = store.GetNextNonBlankRow(10, 5);
        Assert.AreEqual(100, nextRow);
        Assert.AreEqual("B", store.Get(nextRow, 10));
    }

    [Test]
    public void Get_Non_Empty_Rows_Correct()
    {
        IMatrixDataStore<string> store = new SparseMatrixStore<string>();
        store.Set(0,0,"0,0");
        store.Set(2,0,"0,0");
        var nonEmpty = store.GetNonEmptyPositions(0, 2, 0, 0);
        Assert.AreEqual(2, nonEmpty.Count());
    }
    
}