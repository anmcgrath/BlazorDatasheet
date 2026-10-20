using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Edit;
using BlazorDatasheet.Edit.DefaultComponents;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.Lexing;
using Bunit;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Render;

/// <summary>
/// The formula editor is rendered on its own here, as it is when it's used outside of a datasheet.
/// </summary>
public class FormulaEditorTests
{
    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
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
        await using var context = CreateContext();
        var value = "";
        var editor = context.Render<FormulaEditor>(p => p
            .Add(x => x.Sheet, new Sheet(5, 5))
            .Add(x => x.ValueChanged, v => value = v));

        await Type(editor, "=SU", 3);

        value.Should().Be("=SU");
        var suggestions = editor.FindAll(".bds-func-suggestions-name").Select(x => x.TextContent).ToList();
        suggestions.Should().Contain("SUM");
        suggestions.Should().OnlyContain(x => x.StartsWith("SU"));

        await Type(editor, "hello", 5);
        editor.FindAll(".bds-func-suggestions-item").Should().BeEmpty();
    }

    [Test]
    public async Task Keys_Work_The_Suggestions_Before_Anything_Else()
    {
        await using var context = CreateContext();
        var value = "";
        var editor = context.Render<FormulaEditor>(p => p
            .Add(x => x.Sheet, new Sheet(5, 5))
            .Add(x => x.ValueChanged, v => value = v));

        editor.Instance.HandleKey("Enter", false, false, false, false).Should().BeFalse("nothing is suggested");

        await Type(editor, "=1+S", 4);
        var names = editor.FindAll(".bds-func-suggestions-name").Select(x => x.TextContent.Trim()).ToList();
        names.Count.Should().BeGreaterThan(1);
        context.JSInterop.Invocations.Last(x => x.Identifier == "setCaptureListKeys").Arguments.Should().Equal(true);

        await editor.InvokeAsync(() => editor.Instance.HandleKey("ArrowDown", false, false, false, false).Should().BeTrue());
        editor.Find(".bds-func-suggestions-item.bds-active .bds-func-suggestions-name").TextContent.Trim().Should().Be(names[1]);
        await editor.InvokeAsync(() => editor.Instance.HandleKey("ArrowUp", false, false, false, false).Should().BeTrue());
        await editor.InvokeAsync(() => editor.Instance.HandleKey("ArrowDown", false, false, false, false));

        editor.Instance.HandleKey("Enter", false, false, false, false)
            .Should().BeFalse("enter finishes the edit with what was typed, only tab takes a suggestion");

        await editor.InvokeAsync(() => editor.Instance.HandleKey("Tab", false, false, false, false).Should().BeTrue());
        value.Should().Be($"=1+{names[1]}(");
        editor.FindAll(".bds-func-suggestions-item").Should().BeEmpty();
        context.JSInterop.Invocations.Last(x => x.Identifier == "setInputText").Arguments
            .Should().Equal(new object[] { value, value.Length });
        context.JSInterop.Invocations.Last(x => x.Identifier == "setCaptureListKeys").Arguments.Should().Equal(false);
    }

    [Test]
    public async Task Variable_Is_Suggested_And_Written_Without_A_Bracket()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(5, 5);
        sheet.FormulaEngine.SetVariable("sum_of_all1", 5);
        var value = "";
        var editor = context.Render<FormulaEditor>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.ValueChanged, v => value = v));

        await Type(editor, "=1+su", 5);

        var names = editor.FindAll(".bds-func-suggestions-name").Select(x => x.TextContent.Trim()).ToList();
        names.First().Should().Be("sum_of_all1", "names come before functions");
        names.Should().Contain("SUM");

        await editor.InvokeAsync(() => editor.Instance.HandleKey("Tab", false, false, false, false).Should().BeTrue());
        value.Should().Be("=1+sum_of_all1");
    }

    [Test]
    public async Task Provider_Decides_What_Is_Suggested()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(5, 5);
        sheet.FormulaEngine.SetVariable("max_hidden", 5);
        FormulaSuggestionProvider provider = request => request.Defaults
            .Where(x => x.Name != "max_hidden")
            .Prepend(new FormulaSuggestion("max_height", FormulaSuggestionKind.Name, "The height of the couch", "cm"));

        var editor = context.Render<FormulaEditor>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.SuggestionProvider, provider));

        await Type(editor, "=ma", 3);

        var names = editor.FindAll(".bds-func-suggestions-name").Select(x => x.TextContent.Trim()).ToList();
        names.First().Should().Be("max_height");
        names.Should().NotContain("max_hidden");
        names.Should().Contain("MAX");
        editor.Find(".bds-func-suggestions-item.bds-active .bds-func-suggestions-description").TextContent
            .Should().Be("The height of the couch");
        editor.Find(".bds-func-suggestions-item.bds-active .bds-func-suggestions-detail").TextContent.Should().Be("cm");
    }

    [Test]
    public async Task Cascaded_Provider_Suggests_Without_A_Sheet()
    {
        await using var context = CreateContext();
        FormulaSuggestionProvider provider = request =>
            new[] { new FormulaSuggestion("reading", FormulaSuggestionKind.Name) }
                .Where(x => x.Name.StartsWith(request.Prefix));
        var value = "";

        var editor = context.Render<FormulaEditor>(p => p
            .Add(x => x.Sheet, null)
            .Add(x => x.ValueChanged, v => value = v)
            .AddCascadingValue(provider));

        await Type(editor, "=re", 3);
        editor.FindAll(".bds-func-suggestions-name").Select(x => x.TextContent.Trim()).Should().Equal("reading");

        await editor.InvokeAsync(() => editor.Instance.HandleKey("Tab", false, false, false, false));
        value.Should().Be("=reading");
    }

    [Test]
    public async Task Escape_Closes_The_Suggestions_And_Is_Then_Left_To_The_Sheet()
    {
        await using var context = CreateContext();
        var editor = context.Render<FormulaEditor>(p => p.Add(x => x.Sheet, new Sheet(5, 5)));
        await Type(editor, "=SU", 3);

        await editor.InvokeAsync(() => editor.Instance.HandleKey("Escape", false, false, false, false).Should().BeTrue());
        editor.FindAll(".bds-func-suggestions-item").Should().BeEmpty();
        editor.Instance.HandleKey("Escape", false, false, false, false).Should().BeFalse();
    }

    [Test]
    public async Task Clicked_Suggestion_Is_Written_Into_The_Edit_With_The_Caret_After_It()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(5, 5);
        sheet.Editor.BeginEdit(0, 0);
        var editor = context.Render<FormulaEditor>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.ValueChanged, v => sheet.Editor.EditValue = v));

        await Type(editor, "=SUM()+1", 8);
        await Type(editor, "=SU()+1", 3);
        var sum = editor.FindAll(".bds-func-suggestions-item")
            .First(x => x.QuerySelector(".bds-func-suggestions-name")!.TextContent.Trim() == "SUM");
        await editor.InvokeAsync(() => sum.Click());

        sheet.Editor.EditValue.Should().Be("=SUM()+1", "the bracket that was already there is reused");
        sheet.Editor.FormulaEdit.PendingCaret.Should().Be(5);
    }

    [Test]
    public async Task Caret_Inside_A_Function_Shows_Its_Hint()
    {
        await using var context = CreateContext();
        var editor = context.Render<FormulaEditor>(p => p.Add(x => x.Sheet, new Sheet(5, 5)));

        await Type(editor, "=SUM(1,", 7);

        editor.FindComponents<FormulaHintBox>().Should().ContainSingle()
            .Which.Instance.FunctionName.Should().Be("SUM");
    }

    private static Sheet CreateSheetWithDescribedFunctions()
    {
        return new Sheet(5, 5, formulaOptions: new FormulaOptions
        {
            ConfigureFunctions = builder => builder
                .Add(new FunctionDescriptor(
                    "ZZDESCRIBED",
                    [
                        new ParameterDefinition("first", ParameterType.Number, description: "The first number."),
                        new ParameterDefinition("second", ParameterType.Number, ParameterRequirement.Optional),
                        new ParameterDefinition("rest", ParameterType.Number, ParameterRequirement.Optional,
                            isRepeating: true, description: "The other numbers.")
                    ],
                    (_, _) => CellValue.Number(1),
                    description: "Describes itself."))
                .Add(new FunctionDescriptor("ZZPLAIN", [], (_, _) => CellValue.Number(1)))
        });
    }

    [Test]
    public async Task Only_The_Selected_Suggestion_Is_Described()
    {
        await using var context = CreateContext();
        var editor = context.Render<FormulaEditor>(p => p.Add(x => x.Sheet, CreateSheetWithDescribedFunctions()));

        await Type(editor, "=ZZ", 3);

        editor.FindAll(".bds-func-suggestions-name").Select(x => x.TextContent).Should().Equal("ZZDESCRIBED", "ZZPLAIN");
        editor.FindAll(".bds-func-suggestions-description").Should().ContainSingle()
            .Which.TextContent.Should().Be("Describes itself.");

        await editor.InvokeAsync(() => editor.Instance.HandleKey("ArrowDown", false, false, false, false));
        editor.FindAll(".bds-func-suggestions-description").Should().BeEmpty("the selected function has no description");
    }

    [TestCase("=ZZDESCRIBED(", "first", "The first number.")]
    [TestCase("=ZZDESCRIBED(1,2,3,4", "rest", "The other numbers.")]
    public async Task Hint_Describes_The_Function_And_The_Parameter_At_The_Caret(string formula, string paramName,
        string paramDescription)
    {
        await using var context = CreateContext();
        var editor = context.Render<FormulaEditor>(p => p.Add(x => x.Sheet, CreateSheetWithDescribedFunctions()));

        await Type(editor, formula, formula.Length);

        editor.Find(".bds-func-hint-description").TextContent.Should().Be("Describes itself.");
        editor.Find(".bds-func-hint-param-name").TextContent.Trim().Should().StartWith(paramName);
        editor.Find(".bds-func-hint-param-description").TextContent.Should().Be(paramDescription);
    }

    [Test]
    public async Task Hint_Leaves_Out_Descriptions_That_Are_Not_Set()
    {
        await using var context = CreateContext();
        var editor = context.Render<FormulaEditor>(p => p.Add(x => x.Sheet, CreateSheetWithDescribedFunctions()));

        await Type(editor, "=ZZDESCRIBED(1,", 15);
        editor.FindAll(".bds-func-hint-description").Should().ContainSingle();
        editor.FindAll(".bds-func-hint-param").Should().BeEmpty("the second parameter has no description");

        await Type(editor, "=ZZPLAIN(", 9);
        editor.FindAll(".bds-func-hint-signature").Should().ContainSingle();
        editor.FindAll(".bds-func-hint-description").Should().BeEmpty();
    }

    [Test]
    public async Task Named_Ranges_Of_The_Sheet_Are_Highlighted_As_References()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(5, 5);
        sheet.NamedRanges.Set("myName", "B2:B3");
        var editor = context.Render<FormulaEditor>(p => p.Add(x => x.Sheet, sheet));

        await Type(editor, "=myName", 7);

        context.JSInterop.Invocations.Last(x => x.Identifier == "setHighlightHtml").Arguments[0]!.ToString()
            .Should().Contain("color:var(--highlight-color-1)\">myName<");
    }

    [Test]
    [TestCase("=a1:a4")]
    [TestCase("=a1:8888")]
    [TestCase("=a1:8888+B2")]
    public async Task Typing_And_Backspacing_Range_Endpoints_Keeps_All_Text_Visible(string formula)
    {
        await using var context = CreateContext();
        var value = "";
        var editor = context.Render<FormulaEditor>(p => p
            .Add(x => x.Sheet, new Sheet(5, 5))
            .Add(x => x.ValueChanged, v => value = v));
        var parser = new HtmlParser();

        async Task AssertVisible(string text)
        {
            await Type(editor, text, text.Length);

            value.Should().Be(text);
            var html = context.JSInterop.Invocations.Last(x => x.Identifier == "setHighlightHtml")
                .Arguments[0]!.ToString()!;
            using var document = parser.ParseDocument(html);
            document.Body!.TextContent.Should().Be(text);

            if (text == "=a1:a4")
                document.QuerySelectorAll("span[style]").Select(x => x.TextContent)
                    .Should().Equal("a1", ":", "a4");
            if (text == "=a1:a" || text == "=a1:8888")
                document.QuerySelectorAll("span[style]").Select(x => x.TextContent)
                    .Should().Equal("a1");
        }

        for (var length = 1; length <= formula.Length; length++)
            await AssertVisible(formula[..length]);
        for (var length = formula.Length - 1; length >= 0; length--)
            await AssertVisible(formula[..length]);
    }

    [Test]
    public async Task Text_Selection_Is_Reported_To_The_Edit_Session_And_Claims_Input()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(5, 5);
        var editor = context.Render<FormulaEditor>(p => p.Add(x => x.Sheet, sheet));
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
        await using var context = CreateContext();
        var sheet = new Sheet(5, 5);
        sheet.Editor.BeginEdit(0, 0);
        sheet.Editor.EditValue = "=SUM()+1";
        sheet.Editor.FormulaEdit.IsPickingEnabled = true;

        var editor = context.Render<FormulaEditor>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.Value, sheet.Editor.EditValue));
        var input = editor.FindComponent<HighlightedInput>();
        await editor.InvokeAsync(() => input.Instance.HandleSelectionUpdate(5, 5));

        await editor.InvokeAsync(() => sheet.Editor.FormulaEdit.HandlePointerDown(1, 1, false, false, false));
        editor.Render(p => p.Add(x => x.Value, sheet.Editor.EditValue));

        context.JSInterop.Invocations.Last(x => x.Identifier == "setInputText").Arguments
            .Should().Equal(new object[] { "=SUM(B2)+1", 7 });
    }

    [Test]
    public async Task Focus_Returns_To_The_Editor_That_Owns_Input_After_A_Reference_Is_Picked()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(5, 5);
        var inCell = context.Render<FormulaEditor>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.ReadyToFocus, false)
            .Add(x => x.IsDefaultInputOwner, true));
        var external = context.Render<FormulaEditor>(p => p
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

    private static Sheet SheetWithSales()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetValue(0, 0, "Qty");
        sheet.Cells.SetValue(0, 1, "Price");
        sheet.Cells.SetValue(0, 2, "Amount");
        sheet.Workbook.Tables.Add("Sales", sheet.Name, new Region(0, 3, 0, 2)).Should().NotBeNull();
        return sheet;
    }

    private static List<string> SuggestedNames(IRenderedComponent<FormulaEditor> editor) =>
        editor.FindAll(".bds-func-suggestions-name").Select(x => x.TextContent.Trim()).ToList();

    [Test]
    public async Task Tables_Are_Suggested_And_Written_With_A_Bracket()
    {
        await using var context = CreateContext();
        var sheet = SheetWithSales();
        var value = "";
        var editor = context.Render<FormulaEditor>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.ValueChanged, v => value = v));

        await Type(editor, "=Sa", 3);

        SuggestedNames(editor).First().Should().Be("Sales", "tables come before functions");
        editor.Find(".bds-func-suggestions-item.bds-active .bds-func-suggestions-detail").TextContent
            .Should().Be($"{sheet.Name}!A1:C4");

        await editor.InvokeAsync(() => editor.Instance.HandleKey("Tab", false, false, false, false).Should().BeTrue());
        value.Should().Be("=Sales[");
    }

    [Test]
    public async Task Columns_And_Specifiers_Are_Suggested_Inside_The_Brackets()
    {
        await using var context = CreateContext();
        var value = "";
        var editor = context.Render<FormulaEditor>(p => p
            .Add(x => x.Sheet, SheetWithSales())
            .Add(x => x.ValueChanged, v => value = v));

        await Type(editor, "=SUM(Sales[", 11);
        SuggestedNames(editor).Should()
            .Equal("Qty", "Price", "Amount", "#All", "#Data", "#Headers", "#Totals", "#This Row");

        await Type(editor, "=SUM(Sales[Am", 13);
        SuggestedNames(editor).Should().Equal("Amount");

        await editor.InvokeAsync(() => editor.Instance.HandleKey("Tab", false, false, false, false).Should().BeTrue());
        value.Should().Be("=SUM(Sales[Amount]");
    }

    [Test]
    public async Task Column_Accepted_After_Whitespace_Refers_To_The_Column()
    {
        await using var context = CreateContext();
        var sheet = SheetWithSales();
        sheet.Cells.SetValue(1, 2, 5);
        sheet.Cells.SetValue(2, 2, 7);
        var value = "";
        var editor = context.Render<FormulaEditor>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.ValueChanged, v => value = v));

        await Type(editor, "=SUM(Sales[ Am", 14);
        SuggestedNames(editor).Should().Equal("Amount");

        await editor.InvokeAsync(() => editor.Instance.HandleKey("Tab", false, false, false, false).Should().BeTrue());
        value.Should().Be("=SUM(Sales[ Amount]");

        // whitespace inside the brackets is not part of the column name, so the accepted formula works
        sheet.Cells.SetFormula(5, 5, value + ")");
        sheet.Cells.GetValue(5, 5).Should().Be(12);
        sheet.Cells.GetFormulaString(5, 5).Should().Be("=SUM(Sales[Amount])");
    }

    [Test]
    public async Task Column_With_Surrounding_Whitespace_Is_Suggested_Without_It()
    {
        await using var context = CreateContext();
        var sheet = SheetWithSales();
        sheet.Cells.SetValue(0, 2, " Amount ");
        var value = "";
        var editor = context.Render<FormulaEditor>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.ValueChanged, v => value = v));

        await Type(editor, "=SUM(Sales[Am", 13);
        SuggestedNames(editor).Should().Equal("Amount");

        await editor.InvokeAsync(() => editor.Instance.HandleKey("Tab", false, false, false, false).Should().BeTrue());
        value.Should().Be("=SUM(Sales[Amount]");
    }

    [Test]
    public async Task Specifier_Is_Written_With_A_Closing_Bracket_Unless_There_Is_One()
    {
        await using var context = CreateContext();
        var value = "";
        var editor = context.Render<FormulaEditor>(p => p
            .Add(x => x.Sheet, SheetWithSales())
            .Add(x => x.ValueChanged, v => value = v));

        await Type(editor, "=Sales[#H]", 9);
        SuggestedNames(editor).Should().Equal("#Headers");

        await editor.InvokeAsync(() => editor.Instance.HandleKey("Tab", false, false, false, false).Should().BeTrue());
        value.Should().Be("=Sales[#Headers]");
    }

    [Test]
    [TestCase("='Data [West'!A1+SU")]
    [TestCase("='Q[1] data'!A1+SU")]
    public async Task Bracket_In_A_Quoted_Sheet_Name_Does_Not_Stop_Function_Suggestions(string formula)
    {
        await using var context = CreateContext();
        var editor = context.Render<FormulaEditor>(p => p.Add(x => x.Sheet, SheetWithSales()));

        await Type(editor, formula, formula.Length);

        SuggestedNames(editor).Should().Contain("SUM");
        SuggestedNames(editor).Should().OnlyContain(x => x.StartsWith("SU"));
    }

    [Test]
    public async Task Columns_Are_Suggested_After_A_Quoted_Sheet_Name_With_A_Bracket()
    {
        await using var context = CreateContext();
        var editor = context.Render<FormulaEditor>(p => p.Add(x => x.Sheet, SheetWithSales()));

        await Type(editor, "='Data [West'!A1+Sales[Am", 25);

        SuggestedNames(editor).Should().Equal("Amount");
    }

    [Test]
    [TestCase("=SUM(Sales[[#Headers],[Am", "=SUM(Sales[[#Headers],[Amount]]", 31)]
    [TestCase("=Sales[[#Headers],[Am", "=Sales[[#Headers],[Amount]]", 27)]
    [TestCase("=Sales[[Am", "=Sales[[Amount]]", 16)]
    [TestCase("=Sales[Am", "=Sales[Amount]", 14)]
    [TestCase("=Sales[[#He", "=Sales[[#Headers]]", 17)]
    public async Task Accepted_Nested_Item_Closes_The_Reference(string typed, string expected, int caret)
    {
        await using var context = CreateContext();
        var sheet = SheetWithSales();
        sheet.Editor.BeginEdit(5, 5);
        var editor = context.Render<FormulaEditor>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.ValueChanged, v => sheet.Editor.EditValue = v));

        await Type(editor, typed, typed.Length);
        await editor.InvokeAsync(() => editor.Instance.HandleKey("Tab", false, false, false, false).Should().BeTrue());

        sheet.Editor.EditValue.Should().Be(expected);
        sheet.Editor.FormulaEdit.PendingCaret.Should().Be(caret);
        new Lexer().Lex(expected, new FormulaOptions()).Should().NotContain(t => t is BadToken);
    }

    [Test]
    public async Task Unqualified_Reference_Suggests_The_Columns_Of_The_Table_Being_Edited()
    {
        await using var context = CreateContext();
        var sheet = SheetWithSales();
        sheet.Editor.BeginEdit(2, 1);
        var editor = context.Render<FormulaEditor>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.ValueChanged, v => sheet.Editor.EditValue = v));

        await Type(editor, "=[@", 3);
        SuggestedNames(editor).Should().Equal("Qty", "Price", "Amount");
    }

    [Test]
    public async Task Provider_Is_Asked_For_Structured_Reference_Suggestions()
    {
        await using var context = CreateContext();
        FormulaSuggestionRequest? request = null;
        FormulaSuggestionProvider provider = r =>
        {
            request = r;
            return r.Defaults;
        };
        var editor = context.Render<FormulaEditor>(p => p
            .Add(x => x.Sheet, SheetWithSales())
            .Add(x => x.SuggestionProvider, provider));

        await Type(editor, "=sales[P", 8);

        request.Should().NotBeNull();
        request!.IsInStructuredReference.Should().BeTrue();
        request.TableName.Should().Be("Sales");
        request.Prefix.Should().Be("P");
        request.Defaults.Select(x => x.Name).Should().Equal("Price");
    }
}
