using BlazorDatasheet.Core.Formats;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Format;

public class Cell_Format_Tests
{
    [TestCase(nameof(CellFormat.BorderLeft))]
    [TestCase(nameof(CellFormat.BorderRight))]
    [TestCase(nameof(CellFormat.BorderTop))]
    [TestCase(nameof(CellFormat.BorderBottom))]
    public void Partial_Border_Merge_Preserves_Unspecified_Properties_And_Source_Formats(string side)
    {
        var property = typeof(CellFormat).GetProperty(side)!;
        var original = new CellFormat();
        var originalBorder = new Border { Width = 2, Color = "red" };
        property.SetValue(original, originalBorder);
        var merged = original.Clone();
        var incoming = new CellFormat();
        var incomingBorder = new Border { Color = "blue" };
        property.SetValue(incoming, incomingBorder);

        merged.Merge(incoming);
        var result = (Border)property.GetValue(merged)!;
        result.Width.Should().Be(2);
        result.Color.Should().Be("blue");
        result.Should().NotBeSameAs(originalBorder);
        result.Should().NotBeSameAs(incomingBorder);
        originalBorder.Color.Should().Be("red");
        incomingBorder.Width.Should().BeNull();

        property.SetValue(incoming, new Border { Width = 3 });
        merged.Merge(incoming);
        result = (Border)property.GetValue(merged)!;
        result.Width.Should().Be(3);
        result.Color.Should().Be("blue");

        property.SetValue(incoming, null);
        merged.Merge(incoming);
        property.GetValue(merged).Should().BeNull();
    }

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