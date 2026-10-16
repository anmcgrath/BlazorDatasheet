using System.Linq;
using BlazorDatasheet.Menu;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Menu;

public class SheetMenuOptionsTests
{
    private static readonly System.Reflection.PropertyInfo[] Toggles = typeof(SheetMenuOptions)
        .GetProperties()
        .Where(p => p.PropertyType == typeof(bool) && p.CanWrite)
        .ToArray();

    [Test]
    public void Clone_Copies_Every_Toggle()
    {
        foreach (var toggle in Toggles)
        {
            var options = new SheetMenuOptions();
            var flipped = !(bool)toggle.GetValue(options)!;
            toggle.SetValue(options, flipped);

            ((bool)toggle.GetValue(options.Clone())!).Should().Be(flipped, toggle.Name);
        }
    }

    [Test]
    public void CompareTo_Detects_A_Change_To_Every_Toggle()
    {
        foreach (var toggle in Toggles)
        {
            var options = new SheetMenuOptions();
            var changed = new SheetMenuOptions();
            toggle.SetValue(changed, !(bool)toggle.GetValue(changed)!);

            options.CompareTo(changed).Should().BeFalse(toggle.Name);
        }
    }

    [Test]
    public void Paste_Special_Is_Enabled_By_Default()
    {
        new SheetMenuOptions().PasteSpecialEnabled.Should().BeTrue();
    }

    [Test]
    public void Header_Menu_Show_On_Hover_Is_Off_By_Default()
    {
        new SheetMenuOptions().HeaderMenuShowOnHover.Should().BeFalse();
    }
}
