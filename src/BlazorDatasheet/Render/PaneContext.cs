using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Render.Layers;
using BlazorDatasheet.Render.Layers.Preview;
using BlazorDatasheet.Services;
using Microsoft.AspNetCore.Components;

namespace BlazorDatasheet.Render;

internal sealed record PaneContext(
    Sheet Sheet,
    RenderFragment<VisualCell> CellRenderFragment,
    Dictionary<string, CellTypeDefinition> CustomCellTypeDefinitions,
    EventCallback<bool> AutofillDraggingChanged,
    SheetPointerInputService? PointerInputService,
    PreviewService PreviewService,
    int NumberPrecisionDisplay,
    bool ShowFormulaDependents,
    bool UseAutoFill,
    bool IsReadOnly,
    bool AutoFit);
