using BlazorDatasheet.Data;
using BlazorDatasheet.Render;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Layout;

public class CellLayoutProviderTests
{
    [Test]
    public void Setting_Width_Computes_Correct_Column_Width()
    {
        var sheet = new Sheet(3, 3);
        var w1 = 20;
        var w2 = 40;
        var provider = new CellLayoutProvider(sheet, w1, 10);
        Assert.AreEqual(w1, provider.ComputeWidth(1, 1));
        Assert.AreEqual(w1 * 3, provider.ComputeWidth(0, 3));
        provider.SetColumnWidth(1, w2);
        Assert.AreEqual(2 * w1 + w2, provider.ComputeWidth(0, 3));
    }
}