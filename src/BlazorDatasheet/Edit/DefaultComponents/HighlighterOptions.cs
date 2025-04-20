using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorDatasheet.Edit.DefaultComponents;

internal class HighlighterOptions
{
    public required string InitialText { get; init; }
    public required string InitialHtml { get; init; }
    public required ElementReference InputEl { get; init; }
    public required ElementReference HighlightResultEl { get; init; }
    public required DotNetObjectReference<HighlightedInput> DotnetHelper { get; init; }
    /// <summary>
    /// Whether to disable the highlighter input from having arrow key control over the caret position.
    /// Used to enable control of ranges during soft edits.
    /// </summary>
    public required bool PreventDefaultArrowKeys { get; init; }
}