using BlazorDatasheet.Core.Edit;

namespace BlazorDatasheet.Render.Layers;

/// <summary>
/// The start of a drag of a reference that is highlighted on the sheet.
/// </summary>
/// <param name="Mode"></param>
/// <param name="Row">The row that the drag starts from, or null if it is the one at <paramref name="LayerY"/>.</param>
/// <param name="Col">The column that the drag starts from, or null if it is the one at <paramref name="LayerX"/>.</param>
/// <param name="LayerX">The position of the pointer, in px, from the left of the layer.</param>
/// <param name="LayerY">The position of the pointer, in px, from the top of the layer.</param>
public readonly record struct HighlightBoxDrag(
    ReferenceDragMode Mode,
    int? Row,
    int? Col,
    double LayerX,
    double LayerY);
