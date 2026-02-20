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

    [Test]
    public void Insert_Data_At_Large_Sparse_Set_Shifts_All_Data_At_And_After_Index()
    {
        const int insertAt = 7500;
        const int shift = 200;
        var entries = Enumerable.Range(0, 5000)
            .Select(i => (key: i * 3, value: i))
            .ToList();

        foreach (var entry in entries)
            _list.Set(entry.key, entry.value);

        _list.InsertAt(insertAt, shift);

        var expected = entries
            .Select(x => (itemIndex: x.key >= insertAt ? x.key + shift : x.key, data: x.value))
            .OrderBy(x => x.itemIndex)
            .ToList();

        _list.GetNonEmptyData().Should().Equal(expected);
    }

    [Test]
    public void Delete_Data_At_Large_Sparse_Set_Removes_Range_And_Shifts_Following_Data()
    {
        const int deleteAt = 7500;
        const int nItems = 200;
        var deleteEnd = deleteAt + nItems - 1;
        var entries = Enumerable.Range(0, 5000)
            .Select(i => (key: i * 3, value: i))
            .ToList();

        foreach (var entry in entries)
            _list.Set(entry.key, entry.value);

        var deleted = _list.DeleteAt(deleteAt, nItems);

        var expectedDeleted = entries
            .Where(x => x.key >= deleteAt && x.key <= deleteEnd)
            .Select(x => (indexDeleted: x.key, value: x.value))
            .ToList();

        deleted.Should().Equal(expectedDeleted);

        var expectedRemaining = entries
            .Where(x => x.key < deleteAt || x.key > deleteEnd)
            .Select(x => (itemIndex: x.key > deleteEnd ? x.key - nItems : x.key, data: x.value))
            .OrderBy(x => x.itemIndex)
            .ToList();

        _list.GetNonEmptyData().Should().Equal(expectedRemaining);
    }
}
