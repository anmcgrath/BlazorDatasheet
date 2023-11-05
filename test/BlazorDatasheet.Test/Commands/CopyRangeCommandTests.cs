using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Data;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class CopyRangeCommandTests
{
    private Sheet _sheet;

    [SetUp]
    public void Setup_Sheet()
    {
        _sheet = new Sheet(100, 100);
    }

    [Test]
    public void Copy_Region_Copies_Values()
    {
        _sheet.Range("A1:B2").Value = "test";
        _sheet.Range("C1:D2").Value = "prev";
        var cmd = new CopyRangeCommand(_sheet.Range("A1:B2"), _sheet.Range("C1"));
        
        cmd.Execute(_sheet);
        for (int r = 0; r < 2; r++)
        {
            _sheet.Cells[r, 2].Value.Should().Be("test");
            _sheet.Cells[r, 3].Value.Should().Be("test");
        }
        cmd.Undo(_sheet);
        
        for (int r = 0; r < 2; r++)
        {
            _sheet.Cells[r, 2].Value.Should().Be("prev");
            _sheet.Cells[r, 3].Value.Should().Be("prev");
        }
    }
}