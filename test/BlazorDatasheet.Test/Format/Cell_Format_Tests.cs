using BlazorDatasheet.Core.Formats;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Format;

public class Cell_Format_Tests
{
    [Test]
    public void Compare_Cell_Formats_With_Same_Formats_Is_True()
    {
        var f1 = new CellFormat()
        {
            HorizontalTextAlign = TextAlign.End,
            VerticalTextAlign = TextAlign.Center,
        };

        var f2 = new CellFormat()
        {
            HorizontalTextAlign = TextAlign.End,
            VerticalTextAlign = TextAlign.Center,
        };

        f1.Equals(f2).Should().BeTrue();
    }
    
    [Test]
    public void Compare_Cell_Formats_With_Different_Formats_Is_False()
    {
        var f1 = new CellFormat()
        {
            HorizontalTextAlign = TextAlign.End,
        };

        var f2 = new CellFormat()
        {
            VerticalTextAlign = TextAlign.Center,
        };

        f1.Equals(f2).Should().BeFalse();
    }
}