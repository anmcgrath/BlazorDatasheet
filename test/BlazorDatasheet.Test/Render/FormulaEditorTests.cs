using System.Linq;
using System.Threading.Tasks;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Edit;
using BlazorDatasheet.Edit.DefaultComponents;
using Bunit;
using FluentAssertions;
using NUnit.Framework;
using TestContext = Bunit.TestContext;

namespace BlazorDatasheet.Test.Render;

/// <summary>
/// The formula editor is rendered on its own here, as it is when it's used outside of a datasheet.
/// </summary>
public class FormulaEditorTests
{
    private static TestContext CreateContext()
    {
        var context = new TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.SetupModule(x => x.Identifier == "createHighlighter");
        return context;
    }

    private static Task Type(IRenderedComponent<FormulaEditor> editor, string text, int caret)
    {
        var input = editor.FindComponent<HighlightedInput>();
        return editor.InvokeAsync(async () =>
        {
            await input.Instance.HandleInput(text);
            await input.Instance.HandleCaretPositionUpdate(caret);
        });
    }

    [Test]
    public async Task Typing_Reports_The_Value_And_Suggests_Functions()
    {
        using var context = CreateContext();
        var value = "";
        var editor = context.RenderComponent<FormulaEditor>(p => p
            .Add(x => x.Sheet, new Sheet(5, 5))
            .Add(x => x.ValueChanged, v => value = v));

        await Type(editor, "=SU", 3);

        value.Should().Be("=SU");
        var suggestions = editor.FindAll(".bds-func-suggestions-item").Select(x => x.TextContent).ToList();
        suggestions.Should().Contain("SUM");
        suggestions.Should().OnlyContain(x => x.StartsWith("SU"));

        await Type(editor, "hello", 5);
        editor.FindAll(".bds-func-suggestions-item").Should().BeEmpty();
    }

    [Test]
    public async Task Keys_Work_The_Suggestions_Before_Anything_Else()
    {
        using var context = CreateContext();
        var value = "";
        var editor = context.RenderComponent<FormulaEditor>(p => p
            .Add(x => x.Sheet, new Sheet(5, 5))
            .Add(x => x.ValueChanged, v => value = v));

        editor.Instance.HandleKey("Enter", false, false, false, false).Should().BeFalse("nothing is suggested");

        await Type(editor, "=1+S", 4);
        var names = editor.FindAll(".bds-func-suggestions-item").Select(x => x.TextContent.Trim()).ToList();
        names.Count.Should().BeGreaterThan(1);
        context.JSInterop.Invocations.Last(x => x.Identifier == "setCaptureListKeys").Arguments.Should().Equal(true);

        await editor.InvokeAsync(() => editor.Instance.HandleKey("ArrowDown", false, false, false, false).Should().BeTrue());
        editor.Find(".bds-func-suggestions-item.active").TextContent.Trim().Should().Be(names[1]);
        await editor.InvokeAsync(() => editor.Instance.HandleKey("ArrowUp", false, false, false, false).Should().BeTrue());
        await editor.InvokeAsync(() => editor.Instance.HandleKey("ArrowDown", false, false, false, false));

        await editor.InvokeAsync(() => editor.Instance.HandleKey("Tab", false, false, false, false).Should().BeTrue());
        value.Should().Be($"=1+{names[1]}(");
        editor.FindAll(".bds-func-suggestions-item").Should().BeEmpty();
        context.JSInterop.Invocations.Last(x => x.Identifier == "setInputText").Arguments
            .Should().Equal(new object[] { value, value.Length });
        context.JSInterop.Invocations.Last(x => x.Identifier == "setCaptureListKeys").Arguments.Should().Equal(false);
    }

    [Test]
    public async Task Escape_Closes_The_Suggestions_And_Is_Then_Left_To_The_Sheet()
    {
        using var context = CreateContext();
        var editor = context.RenderComponent<FormulaEditor>(p => p.Add(x => x.Sheet, new Sheet(5, 5)));
        await Type(editor, "=SU", 3);

        await editor.InvokeAsync(() => editor.Instance.HandleKey("Escape", false, false, false, false).Should().BeTrue());
        editor.FindAll(".bds-func-suggestions-item").Should().BeEmpty();
        editor.Instance.HandleKey("Escape", false, false, false, false).Should().BeFalse();
    }

    [Test]
    public async Task Clicked_Suggestion_Is_Written_Into_The_Edit_With_The_Caret_After_It()
    {
        using var context = CreateContext();
        var sheet = new Sheet(5, 5);
        sheet.Editor.BeginEdit(0, 0);
        var editor = context.RenderComponent<FormulaEditor>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.ValueChanged, v => sheet.Editor.EditValue = v));

        await Type(editor, "=SUM()+1", 8);
        await Type(editor, "=SU()+1", 3);
        var sum = editor.FindAll(".bds-func-suggestions-item").First(x => x.TextContent.Trim() == "SUM");
        await editor.InvokeAsync(() => sum.Click());

        sheet.Editor.EditValue.Should().Be("=SUM()+1", "the bracket that was already there is reused");
        sheet.Editor.FormulaEdit.PendingCaret.Should().Be(5);
    }

    [Test]
    public async Task Caret_Inside_A_Function_Shows_Its_Hint()
    {
        using var context = CreateContext();
        var editor = context.RenderComponent<FormulaEditor>(p => p.Add(x => x.Sheet, new Sheet(5, 5)));

        await Type(editor, "=SUM(1,", 7);

        editor.FindComponents<FormulaHintBox>().Should().ContainSingle()
            .Which.Instance.FunctionName.Should().Be("SUM");
    }

    [Test]
    public async Task Named_Ranges_Of_The_Sheet_Are_Highlighted_As_References()
    {
        using var context = CreateContext();
        var sheet = new Sheet(5, 5);
        sheet.NamedRanges.Set("myName", "B2:B3");
        var editor = context.RenderComponent<FormulaEditor>(p => p.Add(x => x.Sheet, sheet));

        await Type(editor, "=myName", 7);

        context.JSInterop.Invocations.Last(x => x.Identifier == "setHighlightHtml").Arguments[0]!.ToString()
            .Should().Contain("color:var(--highlight-color-1)\">myName<");
    }

    [Test]
    public async Task Text_Selection_Is_Reported_To_The_Edit_Session_And_Claims_Input()
    {
        using var context = CreateContext();
        var sheet = new Sheet(5, 5);
        var editor = context.RenderComponent<FormulaEditor>(p => p.Add(x => x.Sheet, sheet));
        var input = editor.FindComponent<HighlightedInput>();

        sheet.Editor.BeginEdit(0, 0);
        sheet.Editor.EditValue = "=SUM()+1";

        await editor.InvokeAsync(() => input.Instance.HandleSelectionUpdate(5, 5));
        sheet.Editor.FormulaEdit.SelectionStart.Should().Be(5);
        sheet.Editor.FormulaEdit.InputOwner.Should().BeSameAs(editor.Instance);

        // the selection moves to the sheet while a reference is picked
        await editor.InvokeAsync(() => input.Instance.HandleSelectionUpdate(-1, -1));
        sheet.Editor.FormulaEdit.SelectionStart.Should().Be(5);
    }

    [Test]
    public async Task Picked_Reference_Is_Shown_With_The_Caret_After_It()
    {
        using var context = CreateContext();
        var sheet = new Sheet(5, 5);
        sheet.Editor.BeginEdit(0, 0);
        sheet.Editor.EditValue = "=SUM()+1";
        sheet.Editor.FormulaEdit.IsPickingEnabled = true;

        var editor = context.RenderComponent<FormulaEditor>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.Value, sheet.Editor.EditValue));
        var input = editor.FindComponent<HighlightedInput>();
        await editor.InvokeAsync(() => input.Instance.HandleSelectionUpdate(5, 5));

        await editor.InvokeAsync(() => sheet.Editor.FormulaEdit.HandlePointerDown(1, 1, false, false, false));
        editor.SetParametersAndRender(p => p.Add(x => x.Value, sheet.Editor.EditValue));

        context.JSInterop.Invocations.Last(x => x.Identifier == "setInputText").Arguments
            .Should().Equal(new object[] { "=SUM(B2)+1", 7 });
    }

    [Test]
    public async Task Focus_Returns_To_The_Editor_That_Owns_Input_After_A_Reference_Is_Picked()
    {
        using var context = CreateContext();
        var sheet = new Sheet(5, 5);
        var inCell = context.RenderComponent<FormulaEditor>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.ReadyToFocus, false)
            .Add(x => x.IsDefaultInputOwner, true));
        var external = context.RenderComponent<FormulaEditor>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.ReadyToFocus, false));

        int FocusCount() => context.JSInterop.Invocations.Count(x => x.Identifier == "focusAndMoveCursorTo");

        void Pick()
        {
            sheet.Editor.FormulaEdit.HandlePointerDown(1, 1, false, false, false);
            sheet.Editor.FormulaEdit.HandlePointerUp();
        }

        sheet.Editor.BeginEdit(0, 0);
        sheet.Editor.EditValue = "=";
        sheet.Editor.FormulaEdit.IsPickingEnabled = true;

        await inCell.InvokeAsync(Pick);
        FocusCount().Should().Be(1, "the in-cell editor takes focus when no editor has claimed input");
        context.JSInterop.Invocations.Last(x => x.Identifier == "focusAndMoveCursorTo").Arguments
            .Should().Equal(new object[] { 3 }, "the caret goes after the picked reference");

        sheet.Editor.FormulaEdit.SetInputOwner(external.Instance);
        await inCell.InvokeAsync(Pick);
        FocusCount().Should().Be(2, "only the editor that claimed input takes focus");
    }
}
