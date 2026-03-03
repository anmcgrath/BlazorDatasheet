using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Render.Layers.Preview;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Render;

public class PreviewServiceTests
{
    [Test]
    public void Add_Fires_Changed_And_Item_Appears_In_Items()
    {
        var service = new PreviewService();
        var item = new PreviewItem { Coord = new PixelCoord(0, 0, 10, 10) };
        var changedCount = 0;
        service.Changed += () => changedCount++;

        service.Add(item);

        changedCount.Should().Be(1);
        service.Items.Should().ContainSingle().Which.Should().BeSameAs(item);
    }

    [Test]
    public void Remove_By_Reference_Fires_Changed_And_Item_Is_Gone()
    {
        var service = new PreviewService();
        var item = new PreviewItem { Coord = new PixelCoord(0, 0, 10, 10) };
        service.Add(item);
        var changedCount = 0;
        service.Changed += () => changedCount++;

        service.Remove(item);

        changedCount.Should().Be(1);
        service.Items.Should().BeEmpty();
    }

    [Test]
    public void Clear_Fires_Changed_Once_And_Empties_List()
    {
        var service = new PreviewService();
        service.Add(new PreviewItem { Coord = new PixelCoord(0, 0, 10, 10) });
        service.Add(new PreviewItem { Coord = new PixelCoord(5, 5, 20, 20) });
        var changedCount = 0;
        service.Changed += () => changedCount++;

        service.Clear();

        changedCount.Should().Be(1);
        service.Items.Should().BeEmpty();
    }

    [Test]
    public void Clear_When_Empty_Does_Not_Fire_Changed()
    {
        var service = new PreviewService();
        var changedCount = 0;
        service.Changed += () => changedCount++;

        service.Clear();

        changedCount.Should().Be(0);
    }

    [Test]
    public void Remove_Nonexistent_Item_Does_Not_Fire_Changed()
    {
        var service = new PreviewService();
        var item = new PreviewItem { Coord = new PixelCoord(0, 0, 10, 10) };
        var changedCount = 0;
        service.Changed += () => changedCount++;

        service.Remove(item);

        changedCount.Should().Be(0);
    }

    [Test]
    public void Update_Replaces_Old_Item_And_Fires_Changed_Once()
    {
        var service = new PreviewService();
        var old = new PreviewItem { Coord = new PixelCoord(0, 0, 10, 10) };
        service.Add(old);
        var changedCount = 0;
        service.Changed += () => changedCount++;

        var replacement = new PreviewItem { Coord = new PixelCoord(5, 5, 20, 20) };
        service.Update(old, replacement);

        changedCount.Should().Be(1);
        service.Items.Should().ContainSingle().Which.Should().BeSameAs(replacement);
    }

    [Test]
    public void Update_With_Null_Old_Item_Just_Adds()
    {
        var service = new PreviewService();
        var changedCount = 0;
        service.Changed += () => changedCount++;

        var replacement = new PreviewItem { Coord = new PixelCoord(5, 5, 20, 20) };
        service.Update(null, replacement);

        changedCount.Should().Be(1);
        service.Items.Should().ContainSingle().Which.Should().BeSameAs(replacement);
    }
}
