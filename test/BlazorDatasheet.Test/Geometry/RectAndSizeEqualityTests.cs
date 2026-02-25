using BlazorDatasheet.DataStructures.Geometry;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Geometry;

public class RectAndSizeEqualityTests
{
    [Test]
    public void Rect_With_Same_Values_Are_Equal()
    {
        var a = new Rect(1, 2, 3, 4);
        var b = new Rect(1, 2, 3, 4);

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    [Test]
    public void Size_With_Same_Values_Are_Equal()
    {
        var a = new Size(10, 20);
        var b = new Size(10, 20);

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }
}
