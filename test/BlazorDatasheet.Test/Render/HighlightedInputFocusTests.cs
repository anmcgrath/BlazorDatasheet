using System.Threading.Tasks;
using System.Linq;
using BlazorDatasheet.Edit.DefaultComponents;
using BlazorDatasheet.Formula.Core.Interpreter;
using Bunit;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Render;

public class HighlightedInputFocusTests
{
    [Test]
    public async Task Initial_Focus_Is_Requested_After_Highlighter_Creation_With_Ownership_Check()
    {
        await using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.SetupModule(x => x.Identifier == "createHighlighter");
        context.Render<HighlightedInput>(p => p
            .Add(x => x.Value, "InitialKey")
            .Add(x => x.FormulaOptions, new FormulaOptions()));
        var calls = context.JSInterop.Invocations.ToList();
        var created = calls.FindIndex(x => x.Identifier == "createHighlighter");
        var focused = calls.FindIndex(x => x.Identifier == "focusAndMoveCursorToEnd");
        created.Should().BeGreaterThanOrEqualTo(0);
        focused.Should().BeGreaterThan(created);
        calls[focused].Arguments.Should().Equal(true);
    }
    [Test]
    public async Task Editor_Readiness_Applies_Initial_Key_Before_Requesting_Focus()
    {
        await using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.SetupModule(x => x.Identifier == "createHighlighter");
        var input = context.Render<HighlightedInput>(p => p
            .Add(x => x.Value, "")
            .Add(x => x.ReadyToFocus, false)
            .Add(x => x.FormulaOptions, new FormulaOptions()));
        context.JSInterop.Invocations.Should().NotContain(x => x.Identifier == "focusAndMoveCursorToEnd");
        input.Render(p => p.Add(x => x.Value, "H").Add(x => x.ReadyToFocus, true));
        var calls = context.JSInterop.Invocations.ToList();
        calls.FindIndex(x => x.Identifier == "setInputText").Should().BeLessThan(
            calls.FindIndex(x => x.Identifier == "focusAndMoveCursorToEnd"));
        context.JSInterop.Invocations.Count(x => x.Identifier == "focusAndMoveCursorToEnd").Should().Be(1);
    }

}
