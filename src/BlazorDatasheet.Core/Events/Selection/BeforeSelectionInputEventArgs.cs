using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events.Selection;

/// <summary>
/// A detached selection proposal. Modify the proposal or cancel it rather than calling selection
/// methods from the handler. Invalid replacements are rejected without changing the selection.
/// </summary>
public sealed class BeforeSelectionInputEventArgs : EventArgs
{
    /// <summary>Whether to retain the last accepted selection or drag preview.</summary>
    public bool Cancel { get; set; }

    public SelectionInputKind InputKind { get; }

    /// <summary>Copies of the currently committed selection regions.</summary>
    public IReadOnlyList<IRegion> CurrentRegions { get; }

    public int CurrentActiveRegionIndex { get; }
    public CellPosition CurrentActiveCellPosition { get; }

    /// <summary>A copy of the last accepted drag preview, if any.</summary>
    public IRegion? CurrentSelectingRegion { get; }

    /// <summary>
    /// The complete selection that would result from this input. During a drag, the proposed
    /// active region is the preview; the remaining regions are the other selections.
    /// Ordinary cell regions must cover complete merged cells and lie within the sheet.
    /// Row and column regions retain their unbounded orthogonal axis.
    /// </summary>
    public IReadOnlyList<IRegion> ProposedRegions { get; set; }

    /// <summary>An index in ProposedRegions, or -1 for an empty selection.</summary>
    public int ProposedActiveRegionIndex { get; set; }

    /// <summary>A position inside both the proposed active region and the sheet.</summary>
    public CellPosition ProposedActiveCellPosition { get; set; }

    /// <summary>True when the active region will be previewed until mouse-up.</summary>
    public bool IsDragPreview { get; }

    internal BeforeSelectionInputEventArgs(SelectionInputKind inputKind,
        IReadOnlyList<IRegion> currentRegions, int currentActiveRegionIndex,
        CellPosition currentActiveCellPosition, IRegion? currentSelectingRegion,
        IReadOnlyList<IRegion> proposedRegions, int proposedActiveRegionIndex,
        CellPosition proposedActiveCellPosition, bool isDragPreview)
    {
        InputKind = inputKind;
        CurrentRegions = currentRegions;
        CurrentActiveRegionIndex = currentActiveRegionIndex;
        CurrentActiveCellPosition = currentActiveCellPosition;
        CurrentSelectingRegion = currentSelectingRegion;
        ProposedRegions = proposedRegions;
        ProposedActiveRegionIndex = proposedActiveRegionIndex;
        ProposedActiveCellPosition = proposedActiveCellPosition;
        IsDragPreview = isDragPreview;
    }
}
