using System.Reflection;
using System.Threading.Tasks;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Edit;
using BlazorDatasheet.Edit;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Render;

public class EditorInitializationTests
{
    [Test]
    public async Task Parent_Render_Before_Dynamic_Editor_Exists_Keeps_Initialization_Pending()
    {
        var sheet = new Sheet(1, 1);
        sheet.Editor.BeginEdit(0, 0, true, EditEntryMode.Key, "a");
        var layer = new RenderProbe();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(EditorLayer).GetField("_sheet", flags)!.SetValue(layer, sheet);
        var pending = typeof(EditorLayer).GetProperty("BeginningEdit", flags)!;
        pending.SetValue(layer, true);

        await layer.AfterRender();
        pending.GetValue(layer).Should().Be(true);

        sheet.Editor.CancelEdit();
        await layer.AfterRender();
        pending.GetValue(layer).Should().Be(false, "a finished edit must not initialize on a later render");
    }

    private sealed class RenderProbe : EditorLayer
    {
        public Task AfterRender() => base.OnAfterRenderAsync(false);
    }
}
