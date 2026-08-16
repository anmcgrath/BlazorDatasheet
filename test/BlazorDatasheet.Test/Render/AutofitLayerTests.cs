using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Render;
using BlazorDatasheet.Render.Layers;
using BlazorDatasheet.Services;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Render;

[TestFixture]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class AutofitLayerTests : Bunit.TestContext
{
    private const string AutofitModulePath =
        "./_content/BlazorDatasheet/Render/Layers/AutofitLayer.razor.js";

    [SetUp]
    public void Setup()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IMenuService>(new MenuService(JSInterop.JSRuntime));

        var virtualiseModule = JSInterop.SetupModule(matcher => matcher.Identifier == "getVirtualiser");
        virtualiseModule.Setup<Rect>(invocation => invocation.Identifier == "calculateViewRect")
            .SetResult(new Rect(0, 0, 100000, 100000));
    }

    [TearDown]
    public void TearDown()
    {
        Dispose();
    }

    [Test]
    public async Task Autofit_Processes_Large_Region_In_Bounded_Batches_And_Merges_Changes()
    {
        const int rowCount = AutofitLayer.BatchSize * 2 + 1;
        var sheet = CreateSingleColumnSheet(rowCount);
        var module = JSInterop.SetupModule(AutofitModulePath);
        IRenderedComponent<Datasheet>? cut = null;
        var observedBatchSizes = new List<int>();

        var firstBatch = module.Setup<AutofitDimensionChange[]>("measureAutofitChanges", invocation =>
        {
            var batch = GetRenderedAutofitCells(cut);
            observedBatchSizes.Add(batch.Count);
            return batch.Count > 0 && int.Parse(batch[0].GetAttribute("data-row")!) < AutofitLayer.BatchSize;
        });
        firstBatch.SetResult([
            new AutofitDimensionChange { Index = 0, ExpandTo = 150 }
        ]);

        var laterBatches = module.Setup<AutofitDimensionChange[]>("measureAutofitChanges", invocation =>
        {
            var batch = GetRenderedAutofitCells(cut);
            observedBatchSizes.Add(batch.Count);
            return batch.Count > 0 && int.Parse(batch[0].GetAttribute("data-row")!) >= AutofitLayer.BatchSize;
        });
        laterBatches.SetResult([
            new AutofitDimensionChange { Index = 0, ExpandTo = 180 }
        ]);

        cut = RenderComponent<Datasheet>(parameters => parameters.Add(component => component.Sheet, sheet));
        var layer = cut.FindComponent<AutofitLayer>();

        await cut.InvokeAsync(() =>
            layer.Instance.AutoFit(new Region(0, rowCount - 1, 0, 0), Axis.Col,
                AutofitMethod.ExpandOnly));

        cut.WaitForAssertion(() =>
        {
            (firstBatch.Invocations.Count + laterBatches.Invocations.Count).Should().Be(3);
            sheet.Columns.GetPhysicalWidth(0).Should().Be(180);
            cut.FindAll("div.bds-autoFit").Should().BeEmpty();
        });

        observedBatchSizes.Should().OnlyContain(count => count > 0 && count <= AutofitLayer.BatchSize);
        observedBatchSizes.Should().Contain(AutofitLayer.BatchSize);
        observedBatchSizes.Should().Contain(1);
        sheet.Commands.GetUndoCommands().Should().ContainSingle();

        (await cut.InvokeAsync(sheet.Commands.Undo)).Should().BeTrue();
        sheet.Columns.GetPhysicalWidth(0).Should().Be(105);
    }

    [Test]
    public async Task Autofit_Queues_Overlapping_Requests_In_Order()
    {
        var sheet = CreateSingleColumnSheet(2);
        var module = JSInterop.SetupModule(AutofitModulePath);
        var invocationOrder = new List<string>();

        var columnMeasurement = module.Setup<AutofitDimensionChange[]>("measureAutofitChanges", invocation =>
        {
            var isColumn = invocation.Arguments[1] as string == "col";
            if (isColumn)
                invocationOrder.Add("col");
            return isColumn;
        });

        var rowMeasurement = module.Setup<AutofitDimensionChange[]>("measureAutofitChanges", invocation =>
        {
            var isRow = invocation.Arguments[1] as string == "row";
            if (isRow)
                invocationOrder.Add("row");
            return isRow;
        });
        rowMeasurement.SetResult([
            new AutofitDimensionChange { Index = 1, ExpandTo = 42 }
        ]);

        var cut = RenderComponent<Datasheet>(parameters => parameters.Add(component => component.Sheet, sheet));
        var layer = cut.FindComponent<AutofitLayer>();

        var firstAutofit = cut.InvokeAsync(() =>
            layer.Instance.AutoFit(new Region(0, 0), Axis.Col, AutofitMethod.ExpandOnly));
        cut.WaitForAssertion(() => columnMeasurement.Invocations.Count.Should().Be(1));

        await cut.InvokeAsync(() =>
            layer.Instance.AutoFit(new Region(1, 0), Axis.Row, AutofitMethod.ExpandOnly));

        columnMeasurement.SetResult([
            new AutofitDimensionChange { Index = 0, ExpandTo = 160 }
        ]);
        await firstAutofit;

        cut.WaitForAssertion(() =>
        {
            rowMeasurement.Invocations.Count.Should().Be(1);
            sheet.Columns.GetPhysicalWidth(0).Should().Be(160);
            sheet.Rows.GetPhysicalHeight(1).Should().Be(42);
        });

        invocationOrder.Should().Equal("col", "row");
        sheet.Commands.GetUndoCommands().Should().HaveCount(2);

        (await cut.InvokeAsync(sheet.Commands.Undo)).Should().BeTrue();
        sheet.Rows.GetPhysicalHeight(1).Should().Be(24);
        sheet.Columns.GetPhysicalWidth(0).Should().Be(160);

        (await cut.InvokeAsync(sheet.Commands.Undo)).Should().BeTrue();
        sheet.Columns.GetPhysicalWidth(0).Should().Be(105);
    }

    [Test]
    public async Task Interaction_Autofit_Is_Attached_To_The_Triggering_Format_Command()
    {
        var sheet = CreateSingleColumnSheet(1);
        var module = JSInterop.SetupModule(AutofitModulePath);
        module.Setup<AutofitDimensionChange[]>("measureAutofitChanges", invocation =>
                invocation.Arguments[1] as string == "row")
            .SetResult([
                new AutofitDimensionChange { Index = 0, ExpandTo = 40 }
            ]);

        var cut = RenderComponent<Datasheet>(parameters =>
        {
            parameters.Add(component => component.Sheet, sheet);
            parameters.Add(component => component.AutoFit, true);
        });

        await cut.InvokeAsync(() =>
            sheet.Range(new Region(0, 0)).Format = new CellFormat { FontWeight = "bold" });

        cut.WaitForAssertion(() => sheet.Rows.GetPhysicalHeight(0).Should().Be(40));
        sheet.Commands.GetUndoCommands().Should().ContainSingle();
        sheet.Commands.GetUndoCommands().Single().GetChainedAfterCommands().Should().ContainSingle();

        (await cut.InvokeAsync(sheet.Commands.Undo)).Should().BeTrue();
        sheet.Rows.GetPhysicalHeight(0).Should().Be(24);
        sheet.Cells.GetCell(0, 0).Format.FontWeight.Should().BeNull();
    }

    [Test]
    public async Task Autofit_Prefers_Expansion_And_Uses_Contraction_When_No_Expansion_Is_Required()
    {
        var sheet = new Sheet(1, 2, [[CellValue.Text("A"), CellValue.Text("B")]]);
        var module = JSInterop.SetupModule(AutofitModulePath);
        module.Setup<AutofitDimensionChange[]>("measureAutofitChanges", invocation =>
                invocation.Arguments[1] as string == "col")
            .SetResult([
                new AutofitDimensionChange { Index = 0, ExpandTo = 160, ContractTo = 70 },
                new AutofitDimensionChange { Index = 1, ContractTo = 65 }
            ]);

        var cut = RenderComponent<Datasheet>(parameters => parameters.Add(component => component.Sheet, sheet));
        var layer = cut.FindComponent<AutofitLayer>();

        await cut.InvokeAsync(() =>
            layer.Instance.AutoFit(new Region(0, 0, 0, 1), Axis.Col,
                AutofitMethod.ExpandOrContract));

        cut.WaitForAssertion(() =>
        {
            sheet.Columns.GetPhysicalWidth(0).Should().Be(160);
            sheet.Columns.GetPhysicalWidth(1).Should().Be(65);
        });

        sheet.Commands.GetUndoCommands().Should().ContainSingle();
        (await cut.InvokeAsync(sheet.Commands.Undo)).Should().BeTrue();
        sheet.Columns.GetPhysicalWidth(0).Should().Be(105);
        sheet.Columns.GetPhysicalWidth(1).Should().Be(105);
    }

    [Test]
    public async Task Autofit_Exact_Fit_In_Later_Batch_Prevents_Contraction_And_Undo()
    {
        const int rowCount = AutofitLayer.BatchSize + 1;
        var sheet = CreateSingleColumnSheet(rowCount);
        var module = JSInterop.SetupModule(AutofitModulePath);
        IRenderedComponent<Datasheet>? cut = null;

        module.Setup<AutofitDimensionChange[]>("measureAutofitChanges", _ =>
            {
                var batch = GetRenderedAutofitCells(cut);
                return batch.Count > 0 && int.Parse(batch[0].GetAttribute("data-row")!) <
                    AutofitLayer.BatchSize;
            })
            .SetResult([
                new AutofitDimensionChange { Index = 0, ContractTo = 70 }
            ]);

        module.Setup<AutofitDimensionChange[]>("measureAutofitChanges", _ =>
            {
                var batch = GetRenderedAutofitCells(cut);
                return batch.Count > 0 && int.Parse(batch[0].GetAttribute("data-row")!) >=
                    AutofitLayer.BatchSize;
            })
            .SetResult([
                new AutofitDimensionChange { Index = 0, ContractTo = 105 }
            ]);

        cut = RenderComponent<Datasheet>(parameters => parameters.Add(component => component.Sheet, sheet));
        var layer = cut.FindComponent<AutofitLayer>();

        await cut.InvokeAsync(() =>
            layer.Instance.AutoFit(sheet.Region, Axis.Col, AutofitMethod.ExpandOrContract));

        cut.WaitForAssertion(() => cut.FindAll("div.bds-autoFit").Should().BeEmpty());
        sheet.Columns.GetPhysicalWidth(0).Should().Be(105);
        sheet.Commands.GetUndoCommands().Should().BeEmpty();
    }

    [Test]
    public async Task Autofit_Uses_The_Visual_Size_Of_A_Merged_Cell()
    {
        var sheet = new Sheet(1, 2, [[CellValue.Text("Merged"), CellValue.Empty]]);
        sheet.Cells.Merge(new Region(0, 0, 0, 1));
        var initialUndoCount = sheet.Commands.GetUndoCommands().Count();
        var module = JSInterop.SetupModule(AutofitModulePath);
        IRenderedComponent<Datasheet>? cut = null;
        double? axisSize = null;
        double? cellSize = null;
        var measurement = module.Setup<AutofitDimensionChange[]>("measureAutofitChanges", _ =>
        {
            var cells = GetRenderedAutofitCells(cut);
            if (cells.Count != 1)
                return false;

            axisSize = double.Parse(cells[0].GetAttribute("data-axis-size")!,
                CultureInfo.InvariantCulture);
            cellSize = double.Parse(cells[0].GetAttribute("data-cell-size")!,
                CultureInfo.InvariantCulture);
            return true;
        });
        measurement.SetResult([]);

        cut = RenderComponent<Datasheet>(parameters => parameters.Add(component => component.Sheet, sheet));
        var layer = cut.FindComponent<AutofitLayer>();

        await cut.InvokeAsync(() =>
            layer.Instance.AutoFit(new Region(0, 0, 0, 1), Axis.Col,
                AutofitMethod.ExpandOnly));

        cut.WaitForAssertion(() => measurement.Invocations.Should().ContainSingle());
        axisSize.Should().Be(105);
        cellSize.Should().Be(210);

        cut.WaitForAssertion(() => cut.FindAll("div.bds-autoFit").Should().BeEmpty());
        sheet.Commands.GetUndoCommands().Should().HaveCount(initialUndoCount);
    }

    [Test]
    public async Task Autofit_Empty_Region_Completes_Without_Interop_Or_Undo()
    {
        var sheet = new Sheet(20, 20);
        var module = JSInterop.SetupModule(AutofitModulePath);
        var measurement = module.Setup<AutofitDimensionChange[]>("measureAutofitChanges");
        measurement.SetResult([]);
        var cut = RenderComponent<Datasheet>(parameters => parameters.Add(component => component.Sheet, sheet));
        var layer = cut.FindComponent<AutofitLayer>();

        await cut.InvokeAsync(() =>
            layer.Instance.AutoFit(sheet.Region, Axis.Col, AutofitMethod.ExpandOrContract));

        measurement.Invocations.Should().BeEmpty();
        cut.FindAll("div.bds-autoFit").Should().BeEmpty();
        sheet.Commands.GetUndoCommands().Should().BeEmpty();
    }

    private static Sheet CreateSingleColumnSheet(int rowCount)
    {
        var values = Enumerable.Range(0, rowCount)
            .Select(row => new[] { CellValue.Text($"Cell {row}") })
            .ToArray();
        return new Sheet(rowCount, 1, values);
    }

    private static IReadOnlyList<AngleSharp.Dom.IElement> GetRenderedAutofitCells(
        IRenderedComponent<Datasheet>? cut)
        => cut == null
            ? Array.Empty<AngleSharp.Dom.IElement>()
            : cut.FindAll("div.bds-autoFit");
}
