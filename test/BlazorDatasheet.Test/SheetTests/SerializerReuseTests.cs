using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Serialization.Json;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class SerializerReuseTests
{
    [Test]
    public void One_serializer_serializes_each_workbook_as_a_new_one_would()
    {
        var first = new Workbook();
        first.AddSheet("First", 5, 5).Cells.SetFormula(0, 0, "=1+2");
        var second = new Workbook();
        second.AddSheet("Second", 5, 5).Cells.SetValue(1, 1, "text");

        var serializer = new SheetJsonSerializer();

        serializer.Serialize(first).Should().Be(new SheetJsonSerializer().Serialize(first));
        serializer.Serialize(second).Should().Be(new SheetJsonSerializer().Serialize(second));
        serializer.Serialize(second, writeIndented: true)
            .Should().Be(new SheetJsonSerializer().Serialize(second, writeIndented: true));
        serializer.Warnings.Should().BeEmpty();
    }
}
