using BlazorDatasheet.Core.Events.Selection;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Selecting;

public partial class Selection
{
    private SelectionSnapshot? _inputPreviewSnapshot;
    private bool _isInputProposal;
    private bool _inputSetsActiveCellPosition;

    /// <summary>
    /// Runs the existing geometry/navigation rules on detached state. Only normal sheet input
    /// enters here; public model operations and the formula editor's Selection bypass the hook.
    /// </summary>
    internal void HandleInput(SelectionInputKind kind, Action<Selection> operation)
    {
        if (!ReferenceEquals(this, _sheet.Selection) ||
            (!_sheet.HasSelectionInputHandlers && _inputPreviewSnapshot == null &&
             !_sheet.Protection.RestrictsSelection))
        {
            operation(this);
            return;
        }

        var proposal = new Selection(_sheet)
        {
            _isInputProposal = true,
            _activeRegionIndex = _activeRegionIndex,
            ActiveCellPosition = ActiveCellPosition,
            SelectingRegion = SelectingRegion?.Clone(),
            _selectingMode = _selectingMode,
            _selectingStartPosition = _selectingStartPosition
        };
        proposal._regions.AddRange(CloneRegions());
        operation(proposal);

        if (!SkipLockedCellsDuringNavigation(kind, operation, proposal))
            return;

        var isPreview = proposal.IsSelecting;
        // Commit only on the detached instance to obtain the complete proposed selection.
        if (isPreview)
        {
            proposal.EndSelecting();
            // EndSelecting appends the preview. Preserve a consumer's chosen ordering on
            // subsequent drag updates, even when the preview was moved before other regions.
            if (kind == SelectionInputKind.Drag && _inputPreviewSnapshot != null)
            {
                var preview = proposal._regions[^1];
                proposal._regions.RemoveAt(proposal._regions.Count - 1);
                proposal._activeRegionIndex = _inputPreviewSnapshot.ActiveRegionIndex;
                proposal._regions.Insert(proposal._activeRegionIndex, preview);
            }
        }

        if (!proposal.ConstrainInputProposal())
            return;

        var args = new BeforeSelectionInputEventArgs(kind, CloneRegions(), _activeRegionIndex,
            ActiveCellPosition, SelectingRegion?.Clone(), proposal.CloneRegions(),
            proposal._activeRegionIndex, proposal.ActiveCellPosition, isPreview);
        _sheet.EmitBeforeSelectionInput(args);

        if (args.Cancel || !IsValidInputProposal(args))
            return;

        // Take ownership of copies: retaining event arguments must not allow later state mutation.
        var snapshot = new SelectionSnapshot(args.ProposedActiveRegionIndex,
            args.ProposedRegions.Select(x => x.Clone()).ToList(), args.ProposedActiveCellPosition);
        if (isPreview)
        {
            ApplyInputPreview(snapshot, proposal._selectingMode, proposal._selectingStartPosition);
        }
        else
        {
            if (SelectingRegion != null)
                CancelSelecting();
            ApplyInputSnapshot(snapshot, proposal._inputSetsActiveCellPosition ||
                snapshot.ActiveCellPosition != ActiveCellPosition);
        }
    }

    /// <summary>
    /// Repeats a navigation while it lands on a cell that protection excludes from selection, so
    /// that runs of locked cells are skipped over rather than blocking the keyboard. Returns false
    /// when there is nowhere left to move and the input should be cancelled.
    /// </summary>
    private bool SkipLockedCellsDuringNavigation(SelectionInputKind kind, Action<Selection> operation,
        Selection proposal)
    {
        if (!_sheet.Protection.RestrictsSelection ||
            kind is not (SelectionInputKind.ArrowNavigation or SelectionInputKind.TabEnterNavigation))
            return true;

        var visited = new HashSet<(CellPosition Position, int RegionIndex)>();
        while (!CanSelectActiveCell(proposal))
        {
            // Repeating the same operation is safe: after the first move the active region is a
            // single cell or merge, so navigation only moves the position. At a sheet edge the
            // position stops changing, which the visited set detects.
            if (!visited.Add((proposal.ActiveCellPosition, proposal._activeRegionIndex)) ||
                visited.Count > _sheet.Area)
                return false;
            operation(proposal);
        }

        return true;
    }

    private bool CanSelectActiveCell(Selection proposal)
    {
        var position = proposal.ActiveCellPosition;
        var region = ExpandRegionOverMerges(new Region(position.row, position.col));
        return region != null && _sheet.Protection.CanSelect(region);
    }

    private bool ConstrainInputProposal()
    {
        for (var i = 0; i < _regions.Count; i++)
        {
            var region = _regions[i];
            if (!region.Intersects(_sheet.Region) || _sheet.Area == 0)
                return false;

            var start = _sheet.Region.GetConstrained(region.Start);
            var end = _sheet.Region.GetConstrained(region.End);
            _regions[i] = region switch
            {
                RowRegion => new RowRegion(start.row, end.row),
                ColumnRegion => new ColumnRegion(start.col, end.col),
                _ => new Region(start.row, end.row, start.col, end.col)
            };
        }

        if (ActiveRegion != null)
            ActiveCellPosition = _sheet.Region.GetConstrained(ActiveRegion.GetConstrained(ActiveCellPosition));
        return true;
    }

    private bool IsValidInputProposal(BeforeSelectionInputEventArgs args)
    {
        if (args.ProposedRegions == null)
            return false;
        if (args.ProposedRegions.Count == 0)
            return !args.IsDragPreview && args.ProposedActiveRegionIndex == -1;
        if (args.ProposedActiveRegionIndex < 0 || args.ProposedActiveRegionIndex >= args.ProposedRegions.Count ||
            !_sheet.Region.Contains(args.ProposedActiveCellPosition) || _sheet.Area == 0)
            return false;

        foreach (var region in args.ProposedRegions)
        {
            if (region == null)
                return false;
            var valid = region switch
            {
                RowRegion => region.Top >= 0 && region.Bottom < _sheet.NumRows,
                ColumnRegion => region.Left >= 0 && region.Right < _sheet.NumCols,
                _ => _sheet.Region.Contains(region) && region.Equals(ExpandRegionOverMerges(region))
            };
            if (!valid)
                return false;
        }

        if (_sheet.Protection.RestrictsSelection && !IsAllowedByProtection(args))
            return false;

        return args.ProposedRegions[args.ProposedActiveRegionIndex].Contains(args.ProposedActiveCellPosition);
    }

    private bool IsAllowedByProtection(BeforeSelectionInputEventArgs args)
    {
        // Navigation only ever moves the active cell; other regions may legitimately contain locked
        // cells when they were set programmatically, and tab cycling simply skips over them.
        if (args.InputKind is SelectionInputKind.ArrowNavigation or SelectionInputKind.TabEnterNavigation)
        {
            var position = args.ProposedActiveCellPosition;
            var active = ExpandRegionOverMerges(new Region(position.row, position.col));
            return active != null && _sheet.Protection.CanSelect(active);
        }

        return args.ProposedRegions.All(x => _sheet.Protection.CanSelect(x));
    }

    private void ApplyInputPreview(SelectionSnapshot snapshot, SelectionMode mode, CellPosition start)
    {
        var oldRegions = CloneRegions();
        var oldActiveRegion = ActiveRegion;
        var oldActiveIndex = _activeRegionIndex;
        _regions.Clear();
        _regions.AddRange(snapshot.Regions.Where((_, i) => i != snapshot.ActiveRegionIndex).Select(x => x.Clone()));
        _activeRegionIndex = oldActiveRegion == null ? -1 : _regions.FindIndex(x => x.Equals(oldActiveRegion));
        _inputPreviewSnapshot = snapshot;
        _selectingMode = mode;
        _selectingStartPosition = start;
        SelectingRegion = snapshot.Regions[snapshot.ActiveRegionIndex].Clone();

        if (oldActiveIndex != _activeRegionIndex || !SameRegion(oldActiveRegion, ActiveRegion))
            ActiveRegionChanged?.Invoke(this, new ActiveRegionChangedEvent(oldActiveRegion, ActiveRegion));
        if (!oldRegions.SequenceEqual(_regions))
            EmitSelectionChange(oldRegions);
        EmitSelectingChanged();
    }

    private void ApplyInputSnapshot(SelectionSnapshot snapshot, bool setActiveCell = true)
    {
        var oldRegions = CloneRegions();
        var oldActiveRegion = ActiveRegion;
        var oldActiveIndex = _activeRegionIndex;
        _regions.Clear();
        _regions.AddRange(snapshot.Regions.Select(x => x.Clone()));
        _activeRegionIndex = snapshot.ActiveRegionIndex;
        if (oldActiveIndex != _activeRegionIndex || !SameRegion(oldActiveRegion, ActiveRegion))
            ActiveRegionChanged?.Invoke(this, new ActiveRegionChangedEvent(oldActiveRegion, ActiveRegion));
        if (_regions.Count != 0 && setActiveCell)
            SetActiveCellPosition(snapshot.ActiveCellPosition.row, snapshot.ActiveCellPosition.col);
        EmitSelectionChange(oldRegions);
    }

    private static bool SameRegion(IRegion? left, IRegion? right) =>
        left == null ? right == null : left.Equals(right);
}
