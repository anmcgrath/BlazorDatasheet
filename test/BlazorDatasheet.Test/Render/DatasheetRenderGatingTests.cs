using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Extensions;
using BlazorDatasheet.Render;
using Bunit;
using FluentAssertions;
using NUnit.Framework;
using TestContext = Bunit.TestContext;

namespace BlazorDatasheet.Test.Render;

/// <summary>
/// The datasheet only re-renders when something it shows has changed.
/// </summary>
public class DatasheetRenderGatingTests
{
    private static TestContext CreateContext()
    {
        var context = new TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.SetupModule(x => x.Identifier == "getVirtualiser")
            .Setup<Rect>(x => x.Identifier == "calculateViewRect").SetResult(new Rect(0, 0, 500, 500));
        context.JSInterop.SetupModule(x => x.Identifier == "createWindowEventsService");
        context.Services.AddBlazorDatasheet();
        return context;
    }

    [Test]
    public void Rendering_Host_Again_With_Unchanged_Parameters_Does_Not_Re_Render_Datasheet()
    {
        using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.RenderComponent<Datasheet>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.Theme, "default")
            .Add(x => x.IsReadOnly, false));

        var renderCount = cut.RenderCount;
        // the same parameters the host supplied the first time
        cut.SetParametersAndRender(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.Theme, "default")
            .Add(x => x.IsReadOnly, false));
        cut.RenderCount.Should().Be(renderCount);
    }

    [Test]
    public void Changing_Theme_Re_Renders_Datasheet()
    {
        using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.RenderComponent<Datasheet>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.Theme, "default"));

        var renderCount = cut.RenderCount;
        cut.SetParametersAndRender(p => p.Add(x => x.Theme, "dark"));
        cut.RenderCount.Should().BeGreaterThan(renderCount);
    }

    [Test]
    public void Changing_IsReadOnly_Re_Renders_Datasheet()
    {
        using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.RenderComponent<Datasheet>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.IsReadOnly, false));

        var renderCount = cut.RenderCount;
        cut.SetParametersAndRender(p => p.Add(x => x.IsReadOnly, true));
        cut.RenderCount.Should().BeGreaterThan(renderCount);
    }

    [Test]
    public void Changing_Sheet_Re_Renders_Datasheet()
    {
        using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, sheet));

        var renderCount = cut.RenderCount;
        cut.SetParametersAndRender(p => p.Add(x => x.Sheet, new Sheet(5, 5)));
        cut.RenderCount.Should().BeGreaterThan(renderCount);
    }

    [Test]
    public void Adding_A_Column_Group_Re_Renders_Datasheet()
    {
        using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, sheet));

        var renderCount = cut.RenderCount;
        cut.InvokeAsync(() => sheet.Columns.SetGroup(1, 3, "Q1"));
        cut.RenderCount.Should().BeGreaterThan(renderCount);
    }

    [Test]
    public void Hiding_A_Row_Re_Renders_Datasheet()
    {
        using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, sheet));

        var renderCount = cut.RenderCount;
        cut.InvokeAsync(() => sheet.Rows.Hide(2, 3));
        cut.RenderCount.Should().BeGreaterThan(renderCount);
    }
}
