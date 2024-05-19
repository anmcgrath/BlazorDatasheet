using System.Linq;
using BlazorDatasheet.DataStructures.Store;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Store;

public class SparseListTests
{
    private SparseList<int> _list;

    [SetUp]
    public void Setup()
    {
        _list = new(-1);
    }

    [Test]
    public void Set_Data_Sparse_List_Returns_Data()
    {
        _list.Set(10, 2);
        _list.Get(10).Should().Be(2);
    }

    [Test]
    public void Clear_Data_Sparse_List_Clears_Data()
    {
        _list.Set(10, 2);
        _list.Clear(10);
        _list.Get(10).Should().Be(-1);
    }

    [Test]
    public void Delete_Data_Should_Delete_And_Shift_Indices()
    {
        _list.Set(10, 2);
        _list.Set(11, 3);
        _list.Set(12, 4);
        var deleted = _list.DeleteAt(10, 2);
        deleted.Count().Should().Be(2);
        deleted.Select(x => x.value).Should().BeEquivalentTo(new[] { 2, 3 });
        deleted.Select(x => x.indexDeleted).Should().BeEquivalentTo(new[] { 10, 11 });
        _list.Get(10).Should().Be(4);
        _list.Get(11).Should().Be(-1);
    }

    [Test]
    public void Insert_Data_At_Should_Insert_And_Shift_Indices()
    {
        _list.Set(10, 2);
        _list.Set(11, 3);
        _list.Set(12, 4);
        _list.InsertAt(10, 2);
        _list.Get(12).Should().Be(2);
        _list.Get(13).Should().Be(3);
        _list.Get(14).Should().Be(4);
    }

    [Test]
    public void Get_Non_Empty_Data_Returns_Non_Empty()
    {
        _list.Set(10, 2);
        _list.Set(15, 3);
        _list.Set(20, 4);
        _list.GetNonEmptyDataBetween(10, 20).Should().HaveCount(3);
        _list.GetNonEmptyDataBetween(10, 20).Select(x => x.data).Should().BeEquivalentTo(new[] { 2, 3, 4 });
        _list.GetNonEmptyDataBetween(10, 20).Select(x => x.itemIndex).Should().BeEquivalentTo(new[] { 10, 15, 20 });
    }

    [Test]
    public void Get_Next_Non_Empty_Index_Returns_Non_Empty()
    {
        _list.Set(10, 2);
        _list.Set(15, 3);
        _list.GetNextNonEmptyItemKey(0).Should().Be(10);
        _list.GetNextNonEmptyItemKey(11).Should().Be(15);
        _list.GetNextNonEmptyItemKey(11).Should().Be(15);
    }
}