using System.Linq;
using System.Threading.Tasks;
using BlazorDatasheet.Core.Clipboard;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Extensions;
using BlazorDatasheet.KeyboardInput;
using Bunit;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Render;

public class DatasheetClipboardTests
{
    private BunitContext _context = null!;
    private IRenderedComponent<Datasheet> _component = null!;
    private Sheet _sheet = null!;

    [SetUp]
    public async Task Setup()
    {
        _context = new BunitContext();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        _context.JSInterop.SetupModule(x => x.Identifier == "getVirtualiser")
            .Setup<Rect>(x => x.Identifier == "calculateViewRect").SetResult(new Rect(0, 0, 500, 500));
        _context.JSInterop.SetupModule(x => x.Identifier == "createWindowEventsService");
        _context.JSInterop.Setup<bool>("bdsWriteClipboard", _ => true).SetResult(true);
        _context.Services.AddBlazorDatasheet();

        _sheet = new Sheet(10, 10);
        _sheet.Cells.SetValue(0, 0, 1);
        _sheet.Cells.SetFormula(1, 0, "=A1*2");
        _sheet.SetFormat(new Region(0, 0), new CellFormat { FontWeight = "bold" });

        _component = _context.Render<Datasheet>(p => p.Add(x => x.Sheet, _sheet));
        await _component.InvokeAsync(() =>
            _component.Instance.HandleFocusChanged(new() { Focused = true, Active = true, Version = 1 }));
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.DisposeAsync();
    }

    private async Task On(System.Action action) => await _component.InvokeAsync(action);

    private async Task Shortcut(string code, KeyboardModifiers modifiers) =>
        await _component.InvokeAsync(() => _component.Instance.HandleShortcuts(code, modifiers));

    private (string text, string html) LastWrite()
    {
        var call = _context.JSInterop.Invocations.Last(x => x.Identifier == "bdsWriteClipboard");
        return ((string)call.Arguments[0]!, (string)call.Arguments[1]!);
    }

    [Test]
    public async Task Ctrl_C_Sets_Workbook_Clipboard_And_Writes_Text_And_Html()
    {
        await On(() => _sheet.Selection.Set(new Region(0, 1, 0, 0)));
        await Shortcut("KeyC", KeyboardModifiers.Ctrl);

        var entry = _sheet.Workbook.Clipboard.Current!;
        entry.Mode.Should().Be(ClipboardMode.Copy);
        entry.Source.Region.Should().BeEquivalentTo(new Region(0, 1, 0, 0));

        var (text, html) = LastWrite();
        text.Should().Be("1\r\n2");
        html.Should().Contain(entry.Id.ToString("N")).And.Contain("font-weight:bold").And.Contain("=A1*2");
    }

    [Test]
    public async Task Paste_Of_Own_Content_Is_Rich_And_Selects_The_Pasted_Region()
    {
        await On(() => _sheet.Selection.Set(new Region(0, 1, 0, 0)));
        await Shortcut("KeyC", KeyboardModifiers.Ctrl);
        var (text, html) = LastWrite();

        await On(() => _sheet.Selection.Set(new Region(0, 2)));
        (await _component.InvokeAsync(() => _component.Instance.PasteAsync(text, html))).Should().BeTrue();

        _sheet.Cells.GetValue(0, 2).Should().Be(1);
        _sheet.Cells.GetFormulaString(1, 2).Should().Be("=C1*2");
        _sheet.GetFormat(0, 2).FontWeight.Should().Be("bold");
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(0, 1, 2, 2));
        _sheet.Workbook.Clipboard.IsActive.Should().BeTrue();
    }

    [Test]
    public async Task Paste_Of_Own_Text_Without_Html_Is_Still_Rich()
    {
        await On(() => _sheet.Selection.Set(new Region(0, 1, 0, 0)));
        await Shortcut("KeyC", KeyboardModifiers.Ctrl);
        await On(() => _sheet.Selection.Set(new Region(0, 2)));
        await _component.InvokeAsync(() => _component.Instance.PasteAsync("1\n2", null));
        _sheet.Cells.GetFormulaString(1, 2).Should().Be("=C1*2");
    }

    [Test]
    public async Task Paste_Of_External_Content_Clears_Copy_Mode_And_Pastes_Values_And_Formats()
    {
        await On(() => _sheet.Selection.Set(new Region(0, 1, 0, 0)));
        await Shortcut("KeyC", KeyboardModifiers.Ctrl);

        await On(() => _sheet.Selection.Set(new Region(4, 4)));
        const string html = "<table><tr><td style=\"color:red\">7</td><td>x</td></tr></table>";
        await _component.InvokeAsync(() => _component.Instance.PasteAsync("7\tx", html));

        _sheet.Workbook.Clipboard.IsActive.Should().BeFalse();
        _sheet.Cells.GetValue(4, 4).Should().Be(7);
        _sheet.Cells.GetValue(4, 5).Should().Be("x");
        _sheet.GetFormat(4, 4).ForegroundColor.Should().Be("red");
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(4, 4, 4, 5));

        await On(() => _sheet.Selection.Set(new Region(6, 6)));
        await _component.InvokeAsync(() => _component.Instance.PasteAsync("a\tb\nc", null));
        _sheet.Cells.GetValue(7, 6).Should().Be("c");
    }

    [Test]
    public async Task Shift_Paste_Pastes_Values_Only()
    {
        await On(() => _sheet.Selection.Set(new Region(0, 1, 0, 0)));
        await Shortcut("KeyC", KeyboardModifiers.Ctrl);
        var (text, html) = LastWrite();
        await On(() => _sheet.Selection.Set(new Region(0, 2)));
        await _component.InvokeAsync(() => _component.Instance.PasteAsync(text, html, PasteMode.Values));
        _sheet.Cells.GetValue(1, 2).Should().Be(2);
        _sheet.Cells.HasFormula(1, 2).Should().BeFalse();
        _sheet.GetFormat(0, 2).FontWeight.Should().BeNull();
    }

    [Test]
    public async Task Escape_Clears_Copy_Mode()
    {
        await On(() => _sheet.Selection.Set(new Region(0, 0)));
        await Shortcut("KeyC", KeyboardModifiers.Ctrl);
        _sheet.Workbook.Clipboard.IsActive.Should().BeTrue();
        await Shortcut("Escape", KeyboardModifiers.None);
        _sheet.Workbook.Clipboard.IsActive.Should().BeFalse();
    }

    [Test]
    public async Task Ctrl_X_Then_Paste_Moves_Cells_And_Ends_Copy_Mode()
    {
        await On(() => _sheet.Cells.SetFormula(0, 3, "=A1")); // D1 references the cut cell
        await On(() => _sheet.Selection.Set(new Region(0, 1, 0, 0)));
        await Shortcut("KeyX", KeyboardModifiers.Ctrl);
        _sheet.Workbook.Clipboard.Current!.Mode.Should().Be(ClipboardMode.Cut);
        var (text, html) = LastWrite();

        await On(() => _sheet.Selection.Set(new Region(5, 5)));
        await _component.InvokeAsync(() => _component.Instance.PasteAsync(text, html));

        _sheet.Cells.GetValue(0, 0).Should().BeNull();
        _sheet.Cells.GetValue(5, 5).Should().Be(1);
        _sheet.Cells.GetFormulaString(6, 5).Should().Be("=F6*2");
        _sheet.Cells.GetFormulaString(0, 3).Should().Be("=F6");
        _sheet.Workbook.Clipboard.IsActive.Should().BeFalse();
    }

    [Test]
    public async Task Read_Only_Sheet_Copies_But_Does_Not_Cut_Or_Paste()
    {
        _component.Render(p => p.Add(x => x.Sheet, _sheet).Add(x => x.IsReadOnly, true));
        await On(() => _sheet.Selection.Set(new Region(0, 0)));
        await Shortcut("KeyX", KeyboardModifiers.Ctrl);
        _sheet.Workbook.Clipboard.IsActive.Should().BeFalse();
        await Shortcut("KeyC", KeyboardModifiers.Ctrl);
        _sheet.Workbook.Clipboard.IsActive.Should().BeTrue();
        (await _component.InvokeAsync(() => _component.Instance.PasteAsync("9", null))).Should().BeFalse();
        _sheet.Cells.GetValue(0, 0).Should().Be(1);
    }
}
