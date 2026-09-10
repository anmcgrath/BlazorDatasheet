using System.Linq;
using BlazorDatasheet.Edit.DefaultComponents;
using BlazorDatasheet.Formula.Core.Interpreter;
using Bunit;
using FluentAssertions;
using NUnit.Framework;
using TestContext = Bunit.TestContext;

namespace BlazorDatasheet.Test.Render;

public class HighlightedInputFocusTests
{
    [Test]
    public void Initial_Focus_Is_Requested_After_Highlighter_Creation_With_Ownership_Check()
    {
        using var context = new TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.SetupModule(x => x.Identifier == "createHighlighter");
        context.RenderComponent<HighlightedInput>(p => p
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
    public void Editor_Readiness_Applies_Initial_Key_Before_Requesting_Focus()
    {
        using var context = new TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.SetupModule(x => x.Identifier == "createHighlighter");
        var input = context.RenderComponent<HighlightedInput>(p => p
            .Add(x => x.Value, "")
            .Add(x => x.ReadyToFocus, false)
            .Add(x => x.FormulaOptions, new FormulaOptions()));
        context.JSInterop.Invocations.Should().NotContain(x => x.Identifier == "focusAndMoveCursorToEnd");
        input.SetParametersAndRender(p => p.Add(x => x.Value, "H").Add(x => x.ReadyToFocus, true));
        var calls = context.JSInterop.Invocations.ToList();
        calls.FindIndex(x => x.Identifier == "setInputText").Should().BeLessThan(
            calls.FindIndex(x => x.Identifier == "focusAndMoveCursorToEnd"));
        context.JSInterop.Invocations.Count(x => x.Identifier == "focusAndMoveCursorToEnd").Should().Be(1);
    }

}
