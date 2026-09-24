using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Render.AutoScroll;
using BlazorDatasheet.Render.Layers;
using BlazorDatasheet.Render.Layers.Preview;
using BlazorDatasheet.Services;
using Microsoft.AspNetCore.Components;

namespace BlazorDatasheet.Render;

internal sealed record PaneContext(
    Sheet Sheet,
    RenderFragment<VisualCell> CellRenderFragment,
    Dictionary<string, CellTypeDefinition> CustomCellTypeDefinitions,
    AutoScrollState AutoScrollState,
    SheetPointerInputService? PointerInputService,
    PreviewService PreviewService,
    int NumberPrecisionDisplay,
    NumberOverflowOptions NumberOverflow,
    bool ShowFormula,
    bool ShowFormulaDependents,
    bool UseAutoFill,
    bool IsReadOnly,
    bool AutoFit,
    bool ShowSelectionWhenNotCurrentSheet)
{
    /// <summary>
    /// Whether moving from this context to <paramref name="other"/> changes anything a
    /// <see cref="VisualCell"/> or the cell render fragment is built from. Everything else only
    /// reaches the pane's layers, which re-render without the cells being rebuilt.
    /// </summary>
    internal bool AffectsCells(PaneContext other)
    {
        return !ReferenceEquals(Sheet, other.Sheet) ||
               !ReferenceEquals(CellRenderFragment, other.CellRenderFragment) ||
               NumberPrecisionDisplay != other.NumberPrecisionDisplay ||
               NumberOverflow != other.NumberOverflow ||
               ShowFormula != other.ShowFormula ||
               // the cell fragment renders boolean/select/custom cells with this
               IsReadOnly != other.IsReadOnly ||
               !CustomCellTypesEqual(CustomCellTypeDefinitions, other.CustomCellTypeDefinitions);
    }

    /// <summary>
    /// Compares two custom cell type dictionaries by content. The dictionary is supplied by the
    /// caller, who may hand over a fresh instance holding the same definitions on every one of
    /// their renders; rebuilding every visible cell for that is the wrong trade.
    /// </summary>
    internal static bool CustomCellTypesEqual(
        Dictionary<string, CellTypeDefinition> a,
        Dictionary<string, CellTypeDefinition> b)
    {
        if (ReferenceEquals(a, b))
            return true;

        if (a.Count != b.Count)
            return false;

        foreach (var (key, value) in a)
        {
            // CellTypeDefinition has no value equality, so a definition counts as changed only
            // when the caller supplies a different instance for the same key.
            if (!b.TryGetValue(key, out var other) || !ReferenceEquals(value, other))
                return false;
        }

        return true;
    }
}
