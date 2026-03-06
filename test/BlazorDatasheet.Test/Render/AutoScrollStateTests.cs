using BlazorDatasheet.Render.AutoScroll;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Render;

public class AutoScrollStateTests
{
    [Test]
    public void IsActive_Is_True_When_Any_Source_Is_Active()
    {
        var state = new AutoScrollState();

        state.SetSheetSelectionActive(true);
        state.IsActive.Should().BeTrue();

        state.SetSheetSelectionActive(false);
        state.SetAutofillDragging(true);
        state.IsActive.Should().BeTrue();

        state.SetAutofillDragging(false);
        state.SetEditorSelectionActive(true);
        state.IsActive.Should().BeTrue();
    }

    [Test]
    public void IsActive_Is_False_When_All_Sources_Are_Inactive()
    {
        var state = new AutoScrollState();

        state.SetSheetSelectionActive(true);
        state.SetAutofillDragging(true);
        state.SetEditorSelectionActive(true);

        state.SetSheetSelectionActive(false);
        state.SetAutofillDragging(false);
        state.SetEditorSelectionActive(false);

        state.IsActive.Should().BeFalse();
    }

    [Test]
    public void Idempotent_Setters_Do_Not_Raise_Changed()
    {
        var state = new AutoScrollState();
        var changedCount = 0;
        state.Changed += () => changedCount++;

        state.SetSheetSelectionActive(false);
        state.SetAutofillDragging(false);
        state.SetEditorSelectionActive(false);
        state.SetSheetSelectionActive(true);
        state.SetSheetSelectionActive(true);
        state.SetAutofillDragging(true);
        state.SetAutofillDragging(true);
        state.SetEditorSelectionActive(true);
        state.SetEditorSelectionActive(true);

        changedCount.Should().Be(3);
    }
}
