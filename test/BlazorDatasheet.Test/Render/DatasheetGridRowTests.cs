using System.Collections.Generic;
using Bunit;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Render;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using NUnit.Framework;
using BunitTestContext = Bunit.TestContext;

namespace BlazorDatasheet.Test.Render;

public class DatasheetGridRowTests
{
    [Test]
    public void Ordinary_Cells_Render_Directly_Without_Per_Cell_Grid_Wrappers()
    {
        using var context = new BunitTestContext();
        var sheet = new Sheet(1, 3);

        var row = RenderRow(context, sheet);

        row.FindAll("[data-test-cell]").Should().HaveCount(3);
        row.FindAll("div").Should().HaveCount(3);
        row.FindAll("[style*='grid-column']").Should().BeEmpty();
    }

    [Test]
    public void Merged_Cell_Gaps_Use_Placeholders_So_Following_Cells_Keep_Their_Track()
    {
        using var context = new BunitTestContext();
        var sheet = new Sheet(1, 3);
        sheet.Cells.Merge(new Region(0, 0, 0, 1));

        var row = RenderRow(context, sheet);

        row.FindAll("[data-test-cell]").Should().HaveCount(2);
        row.FindAll(".bds-merged-cell-placeholder").Should().ContainSingle();
        row.FindAll("div").Should().HaveCount(3);
    }

    private static IRenderedComponent<DatasheetGridRow> RenderRow(BunitTestContext context, Sheet sheet)
    {
        RenderFragment<VisualCell> cellTemplate = cell => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "data-test-cell", cell.Col);
            builder.CloseElement();
        };

        return context.RenderComponent<DatasheetGridRow>(parameters => parameters
            .Add(x => x.Row, 0)
            .Add(x => x.IsDirty, true)
            .Add(x => x.Sheet, sheet)
            .Add(x => x.VisibleColIndices, [0, 1, 2])
            .Add(x => x.Cache, new Dictionary<CellPosition, VisualCell>())
            .Add(x => x.CellRenderFragment, cellTemplate)
            .Add(x => x.NumberPrecisionDisplay, 12));
    }
}
