using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Events.Data;
using BlazorDatasheet.Core.Formats.DefaultConditionalFormats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Core.Formats;

public class ConditionalFormatManager
{
    private readonly Sheet _sheet;

    private readonly List<ConditionalFormatAbstractBase> _registered = new();
    private int _nextOrder;
    private readonly ConsolidatedDataStore<ConditionalFormatAbstractBase> _appliedFormats = new();

    /// <summary>
    /// The regions that each formula conditional format depends on, including precedents outside of
    /// the regions the format is applied to.
    /// </summary>
    private readonly Dictionary<FormulaConditionalFormat, List<IRegion>> _footprints = new();

    public ConditionalFormatManager(Sheet sheet,
        CellStore cellStore)
    {
        _sheet = sheet;
        cellStore.CellsChanged += HandleCellsChanged;
    }

    /// <summary>
    /// Applies the conditional format specified by "key" to all cells in a region
    /// </summary>
    /// <param name="region"></param>
    /// <param name="conditionalFormat"></param>
    public void Apply(IRegion region, ConditionalFormatAbstractBase conditionalFormat)
    {
        Apply(new SheetRange(_sheet, region), conditionalFormat);
    }

    /// <summary>
    /// Applies the conditional format to the region
    /// </summary>
    /// <param name="range"></param>
    /// <param name="conditionalFormat"></param>
    public void Apply(SheetRange? range, ConditionalFormatAbstractBase conditionalFormat)
    {
        if (range == null)
            return;

        var isNewRegistration = !_registered.Contains(conditionalFormat);
        if (isNewRegistration)
        {
            _registered.Add(conditionalFormat);
            conditionalFormat.Order = _nextOrder++;
        }

        _appliedFormats.Add(range.Region, conditionalFormat);

        if (conditionalFormat is FormulaConditionalFormat formulaCf)
        {
            formulaCf.EnsureParsed(_sheet);
            UpdateAnchor(formulaCf, rebaseFormula: !isNewRegistration);
        }

        _footprints.Clear();
        Prepare(new List<ConditionalFormatAbstractBase>() { conditionalFormat });
    }

    /// <summary>
    /// Removes the conditional format from every region it is applied to, and repaints those regions.
    /// Does nothing if the format is not applied.
    /// </summary>
    /// <param name="conditionalFormat"></param>
    public void Remove(ConditionalFormatAbstractBase conditionalFormat)
    {
        if (!_registered.Remove(conditionalFormat))
            return;

        var regions = _appliedFormats.GetRegions(conditionalFormat).ToList();
        foreach (var region in regions)
            _appliedFormats.Clear(region, conditionalFormat);

        _footprints.Clear();
        _sheet.MarkDirty(regions);
    }

    private List<SheetRange> GetRangesAppliedToFormat(ConditionalFormatAbstractBase format)
    {
        var regions = _appliedFormats.GetRegions(format);
        return regions.Select(x => new SheetRange(_sheet, x)).ToList();
    }

    public IEnumerable<DataRegion<ConditionalFormatAbstractBase>> GetAllFormats()
    {
        return _appliedFormats.GetDataRegions(_sheet.Region);
    }

    private IEnumerable<FormulaConditionalFormat> FormulaFormats => _registered.OfType<FormulaConditionalFormat>();

    /// <summary>
    /// Returns the top left of the bounding box of every region the format is applied to,
    /// or null if it is not applied anywhere.
    /// </summary>
    private CellPosition? GetAnchor(ConditionalFormatAbstractBase format)
    {
        var top = int.MaxValue;
        var left = int.MaxValue;
        foreach (var region in _appliedFormats.GetRegions(format))
        {
            top = Math.Min(top, region.Top);
            left = Math.Min(left, region.Left);
        }

        return top == int.MaxValue ? null : new CellPosition(top, left);
    }

    /// <summary>
    /// Re-derives the anchor from the applied regions. When <paramref name="rebaseFormula"/> is set, the
    /// formula's references are shifted by the anchor delta so that every cell keeps evaluating the same
    /// reference as it did before the anchor moved.
    /// </summary>
    private void UpdateAnchor(FormulaConditionalFormat format, bool rebaseFormula)
    {
        var anchor = GetAnchor(format);
        if (anchor == null)
            return;

        if (rebaseFormula)
            format.ShiftRelative(anchor.Value.row - format.Anchor.row, anchor.Value.col - format.Anchor.col, _sheet);

        format.Anchor = anchor.Value;
    }

    private void UpdateAnchors()
    {
        foreach (var format in FormulaFormats)
            UpdateAnchor(format, rebaseFormula: false);
    }

    private void HandleCellsChanged(object? sender, CellDataChangedEventArgs args)
    {
        if (!_appliedFormats.Any())
            return;

        // Simply prepare all cells that the conditional format belongs to (if shared)
        var cellCfs =
            args.Positions
                .SelectMany(x => GetFormatsAppliedToPosition(x.row, x.col));

        var rangeCfs = args.Regions
            .SelectMany(GetFormatsAppliedToRegion);

        var cfs = cellCfs.Concat(rangeCfs)
            .Distinct()
            .ToList();

        Prepare(cfs);
        MarkFormulaFormatsDirty(args);
    }

    /// <summary>
    /// Formula conditional formats can depend on cells outside the region they are applied to,
    /// e.g. =$B1>0 applied to A1:A3 depends on B1:B3.
    /// </summary>
    private void MarkFormulaFormatsDirty(CellDataChangedEventArgs args)
    {
        List<CellPosition>? positions = null;
        List<IRegion>? regions = null;

        foreach (var format in FormulaFormats)
        {
            var footprint = GetFootprint(format);
            if (footprint.Count == 0)
                continue;

            positions ??= args.Positions.ToList();
            regions ??= args.Regions.ToList();

            var intersects = footprint.Any(f =>
                positions.Any(p => f.Contains(p.row, p.col)) ||
                regions.Any(r => f.Intersects(r)));

            if (intersects)
                _sheet.MarkDirty(_appliedFormats.GetRegions(format));
        }
    }

    private List<IRegion> GetFootprint(FormulaConditionalFormat format)
    {
        if (_footprints.TryGetValue(format, out var cached))
            return cached;

        var footprint = new List<IRegion>();
        var bounds = GetBounds(format);
        if (bounds != null)
        {
            foreach (var reference in format.References)
            {
                if (reference.SheetName != _sheet.Name || reference.IsInvalid)
                    continue;

                if (reference.Kind != ReferenceKind.Cell && reference.Kind != ReferenceKind.Range)
                    continue;

                footprint.Add(GetReadRegion(reference, bounds));
            }
        }

        _footprints[format] = footprint;
        return footprint;
    }

    /// <summary>
    /// The cells a reference of a formula format reads across the format's cells, whose bounding region is
    /// <paramref name="bounds"/>: relative parts reach as far as the format spreads.
    /// </summary>
    private static IRegion GetReadRegion(Reference reference, IRegion bounds)
    {
        var extraRows = bounds.Height - 1;
        var extraCols = bounds.Width - 1;
        var region = reference.Region.Clone();
        // an unbounded edge already covers everything the shift could reach.
        if (extraRows > 0 && IsRowRelative(reference) && region.Bottom != int.MaxValue)
            region.Expand(Edge.Bottom, extraRows);
        if (extraCols > 0 && IsColRelative(reference) && region.Right != int.MaxValue)
            region.Expand(Edge.Right, extraCols);
        return region;
    }

    private IRegion? GetBounds(ConditionalFormatAbstractBase format)
    {
        IRegion? bounds = null;
        foreach (var region in _appliedFormats.GetRegions(format))
            bounds = bounds == null ? region.Clone() : bounds.GetBoundingRegion(region);

        return bounds;
    }

    private static bool IsRowRelative(Reference reference) => reference switch
    {
        CellReference cellReference => !cellReference.IsRowFixed,
        RangeReference rangeReference => !rangeReference.IsStartRowFixed || !rangeReference.IsEndRowFixed,
        _ => false
    };

    private static bool IsColRelative(Reference reference) => reference switch
    {
        CellReference cellReference => !cellReference.IsColFixed,
        RangeReference rangeReference => !rangeReference.IsStartColFixed || !rangeReference.IsEndColFixed,
        _ => false
    };

    private void Prepare(List<ConditionalFormatAbstractBase> formats)
    {
        foreach (var format in formats)
        {
            // prepare format (re-compute shared format cache etch.)
            if (format.IsShared)
            {
                format.Prepare(GetRangesAppliedToFormat(format));
                _sheet.MarkDirty(GetRangesAppliedToFormat(format).Select(x => x.Region));
            }
        }
    }

    private IEnumerable<ConditionalFormatAbstractBase> GetFormatsAppliedToPosition(int row, int col)
    {
        return _appliedFormats.GetData(row, col);
    }

    private IEnumerable<ConditionalFormatAbstractBase> GetFormatsAppliedToRegion(IRegion region)
    {
        return _appliedFormats.GetData(region);
    }

    /// <summary>
    /// Applies the conditional format specified by "key" to a particular cell. If setting the format to a number of cells,
    /// prefer setting via a region.
    /// <param name="format"></param>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// </summary>
    public void Apply(int row, int col, ConditionalFormatAbstractBase format)
    {
        Apply(new Region(row, col), format);
    }

    /// <summary>
    /// Whether any conditional format is applied anywhere on the sheet.
    /// </summary>
    internal bool HasAppliedFormats => !_appliedFormats.IsEmpty;

    /// <summary>
    /// Returns the format that results from applying all conditional formats to this cell
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public CellFormat? GetFormatResult(int row, int col)
    {
        // called for every cell that scrolls into view, and most sheets have no conditional
        // formats at all - skip the region check and the store probe entirely in that case.
        if (_appliedFormats.IsEmpty)
            return null;

        if (!_sheet.ContainsPosition(row, col))
            return null;

        var cfs = GetFormatsAppliedToPosition(row, col);
        CellFormat? initialFormat = null;
        foreach (var format in cfs)
        {
            var apply = format.Predicate?.Invoke(new CellPosition(row, col), _sheet);
            if (apply == false)
                continue;
            var calced = format.CalculateFormat(row, col, _sheet);
            if (initialFormat == null)
                initialFormat = calced;
            else
                initialFormat.Merge(calced);
            if (apply == true && format.StopIfTrue)
                break;
        }

        return initialFormat;
    }


    internal ConditionalFormatRestoreData InsertRowColAt(int row, int nRows, Axis axis)
    {
        var formulas = new List<(FormulaConditionalFormat, string)>();
        foreach (var format in FormulaFormats)
        {
            formulas.Add((format, format.FormulaText));
            format.InsertRowCol(row, nRows, axis, _sheet);
        }

        var regions = _appliedFormats.InsertRowColAt(row, nRows, axis);
        UpdateAnchors();
        _footprints.Clear();

        return new ConditionalFormatRestoreData() { Regions = regions, Formulas = formulas };
    }

    internal ConditionalFormatRestoreData RemoveRowColAt(int index, int count, Axis axis)
    {
        IRegion dataRegion =
            axis == Axis.Row ? new RowRegion(index, int.MaxValue) : new ColumnRegion(index, int.MaxValue);

        var cfsAffected = _appliedFormats
            .GetData(dataRegion)
            .ToList();

        var formulas = new List<(FormulaConditionalFormat, string)>();
        foreach (var format in FormulaFormats)
        {
            formulas.Add((format, format.FormulaText));

            var anchorIndex = axis == Axis.Row ? format.Anchor.row : format.Anchor.col;
            if (anchorIndex >= index && anchorIndex < index + count && StillHasCellsAfterRemoval(format, index, count, axis))
            {
                // the anchor itself is being deleted - re-base the rule onto the first surviving cell,
                // which is the cell the region store will leave at the top left.
                var delta = (index + count) - anchorIndex;
                format.ShiftRelative(axis == Axis.Row ? delta : 0, axis == Axis.Col ? delta : 0, _sheet);
                format.Anchor = axis == Axis.Row
                    ? new CellPosition(index + count, format.Anchor.col)
                    : new CellPosition(format.Anchor.row, index + count);
            }

            format.RemoveRowCol(index, count, axis, _sheet);
        }

        var regions = _appliedFormats.RemoveRowColAt(index, count, axis);
        UpdateAnchors();
        _footprints.Clear();
        Prepare(cfsAffected);

        return new ConditionalFormatRestoreData() { Regions = regions, Formulas = formulas };
    }

    internal ConditionalFormatRestoreData InsertCellsAt(IRegion region, Axis axis)
        => ShiftCells(region, axis, _sheet.Name, true);

    internal ConditionalFormatRestoreData RemoveCellsAt(IRegion region, Axis axis)
        => ShiftCells(region, axis, _sheet.Name, false);

    internal ConditionalFormatRestoreData ShiftCells(IRegion region, Axis axis, string sourceSheetName, bool insert)
    {
        var local = sourceSheetName == _sheet.Name;
        var n = axis == Axis.Col ? region.Width : region.Height;
        var index = region.GetLeadingEdgeOffset(axis);
        var end = region.GetTrailingEdgeOffset(axis);
        if (!insert)
            return ShiftCellsInBand(region, axis, sourceSheetName,
                () => local ? _appliedFormats.RemoveCellsAt(region, axis, StraddlePolicy.Split) : new(),
                (format, sheet) => format.RemoveCells(region, axis, sheet, sourceSheetName),
                shift: p => !local ? p : region.Contains(p.row, p.col)
                    ? null
                    : IsInBand(p, region, axis) && Along(p, axis) > end ? Offset(p, axis, -n) : p,
                unshift: p => local && IsInBand(p, region, axis) && Along(p, axis) >= index
                    ? Offset(p, axis, n) : p);

        return ShiftCellsInBand(region, axis, sourceSheetName,
            () => local ? _appliedFormats.InsertCellsAt(region, axis, StraddlePolicy.Split) : new(),
            (format, sheet) => format.InsertCells(region, axis, sheet, sourceSheetName),
            shift: p => local && IsInBand(p, region, axis) && Along(p, axis) >= index ? Offset(p, axis, n) : p,
            unshift: p => local && IsInBand(p, region, axis) && Along(p, axis) >= index + n ? Offset(p, axis, -n) : p);
    }

    /// <summary>
    /// Shifts the applied regions and the formula references for an insertion or removal of cells within a band.
    /// A formula is written relative to its anchor, so when the anchor does not land where the shift takes the old
    /// anchor (it was removed, or the top-left of the applied regions changed), the formula is first re-based onto the
    /// pre-shift position of the new anchor, so every surviving cell keeps evaluating the same references.
    /// </summary>
    /// <param name="shift">Where a pre-shift position ends up, or null if it is removed.</param>
    /// <param name="unshift">Where a post-shift position was before the shift.</param>
    private ConditionalFormatRestoreData ShiftCellsInBand(IRegion region, Axis axis, string sourceSheetName,
        Func<RegionRestoreData<ConditionalFormatAbstractBase>> shiftRegions,
        Action<FormulaConditionalFormat, Sheet> adjustReferences,
        Func<CellPosition, CellPosition?> shift,
        Func<CellPosition, CellPosition> unshift)
    {
        var restore = new ConditionalFormatRestoreData
        {
            Formulas = FormulaFormats.Select(format => (format, format.FormulaText)).ToList()
        };
        SplitFormulaFormats(region, sourceSheetName, restore);
        var formulaFormats = FormulaFormats.ToList();

        var regions = shiftRegions();

        foreach (var format in formulaFormats)
        {
            var newAnchor = GetAnchor(format);
            if (newAnchor == null || shift(format.Anchor) == newAnchor)
                continue;

            var rebaseTo = unshift(newAnchor.Value);
            format.ShiftRelative(rebaseTo.row - format.Anchor.row, rebaseTo.col - format.Anchor.col, _sheet);
            format.Anchor = rebaseTo;
        }

        foreach (var format in formulaFormats)
        {
            adjustReferences(format, _sheet);
            _sheet.MarkDirty(_appliedFormats.GetRegions(format));
        }

        UpdateAnchors();
        _footprints.Clear();
        Prepare(regions.RegionsAdded.Select(x => x.Data)
            .Concat(regions.RegionsRemoved.Select(x => x.Data))
            .Distinct()
            .ToList());

        return new ConditionalFormatRestoreData
        {
            Regions = regions,
            Formulas = restore.Formulas,
            SplitRegions = restore.SplitRegions,
            SplitFormats = restore.SplitFormats
        };
    }

    private void SplitFormulaFormats(IRegion shifted, string sourceSheetName, ConditionalFormatRestoreData restore)
    {
        foreach (var format in FormulaFormats.ToList())
        {
            var rowCuts = new SortedSet<int>();
            var colCuts = new SortedSet<int>();
            if (sourceSheetName == _sheet.Name)
            {
                rowCuts.UnionWith(new[] { shifted.Top, shifted.Bottom + 1 });
                colCuts.UnionWith(new[] { shifted.Left, shifted.Right + 1 });
            }

            foreach (var reference in format.References)
            {
                if (reference.SheetName != sourceSheetName || reference.IsInvalid)
                    continue;
                if (reference is CellReference cell)
                {
                    AddCuts(rowCuts, cell.RowIndex, cell.IsRowFixed, format.Anchor.row, shifted.Top, shifted.Bottom);
                    AddCuts(colCuts, cell.ColIndex, cell.IsColFixed, format.Anchor.col, shifted.Left, shifted.Right);
                }
                else if (reference is RangeReference range && range.Region is not RowRegion and not ColumnRegion)
                {
                    AddCuts(rowCuts, range.Region.Top, range.IsStartRowFixed, format.Anchor.row, shifted.Top, shifted.Bottom);
                    AddCuts(rowCuts, range.Region.Bottom, range.IsEndRowFixed, format.Anchor.row, shifted.Top, shifted.Bottom);
                    AddCuts(colCuts, range.Region.Left, range.IsStartColFixed, format.Anchor.col, shifted.Left, shifted.Right);
                    AddCuts(colCuts, range.Region.Right, range.IsEndColFixed, format.Anchor.col, shifted.Left, shifted.Right);
                }
            }

            var pieces = _appliedFormats.GetRegions(format)
                .SelectMany(applied => Partition(applied, rowCuts, colCuts)).ToList();
            if (pieces.Count <= 1)
                continue;

            var template = (FormulaConditionalFormat)format.Clone();
            restore.SplitRegions.Add(_appliedFormats.Clear(new AllRegion(), format));
            for (var index = 0; index < pieces.Count; index++)
            {
                var piece = pieces[index];
                var split = index == 0 ? format : (FormulaConditionalFormat)template.Clone();
                split.EnsureParsed(_sheet);
                split.ShiftRelative(piece.Top - template.Anchor.row, piece.Left - template.Anchor.col, _sheet);
                split.Anchor = piece.TopLeft;
                if (index > 0)
                {
                    _registered.Add(split);
                    restore.SplitFormats.Add(split);
                }
                restore.SplitRegions.Add(_appliedFormats.Add(piece, split));
            }
        }
    }

    private static void AddCuts(SortedSet<int> cuts, int coordinate, bool fixedCoordinate, int anchor,
        int start, int end)
    {
        if (fixedCoordinate)
            return;
        foreach (var boundary in new[] { (long)start, (long)end + 1 })
        {
            var cut = boundary - coordinate + anchor;
            if (cut >= 0 && cut < int.MaxValue)
                cuts.Add((int)cut);
        }
    }

    private static IEnumerable<IRegion> Partition(IRegion region, SortedSet<int> rowCuts, SortedSet<int> colCuts)
    {
        var rows = new[] { (long)region.Top }.Concat(rowCuts.Where(cut => cut > region.Top && cut <= region.Bottom)
            .Select(cut => (long)cut)).Append((long)region.Bottom + 1).ToArray();
        var cols = new[] { (long)region.Left }.Concat(colCuts.Where(cut => cut > region.Left && cut <= region.Right)
            .Select(cut => (long)cut)).Append((long)region.Right + 1).ToArray();
        for (var row = 0; row < rows.Length - 1; row++)
        for (var col = 0; col < cols.Length - 1; col++)
            yield return new Region((int)rows[row], (int)(rows[row + 1] - 1),
                (int)cols[col], (int)(cols[col + 1] - 1));
    }

    private static bool IsInBand(CellPosition p, IRegion region, Axis axis) => axis == Axis.Col
        ? p.row >= region.Top && p.row <= region.Bottom
        : p.col >= region.Left && p.col <= region.Right;

    private static int Along(CellPosition p, Axis axis) => axis == Axis.Col ? p.col : p.row;

    private static CellPosition Offset(CellPosition p, Axis axis, int amount) => axis == Axis.Col
        ? new CellPosition(p.row, p.col + amount)
        : new CellPosition(p.row + amount, p.col);

    private bool StillHasCellsAfterRemoval(ConditionalFormatAbstractBase format, int index, int count, Axis axis)
    {
        foreach (var region in _appliedFormats.GetRegions(format))
        {
            var start = axis == Axis.Row ? region.Top : region.Left;
            var end = axis == Axis.Row ? region.Bottom : region.Right;
            if (start < index || end >= index + count)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Moves the conditional formats applied in <paramref name="from"/> to the block of the same size at
    /// <paramref name="to"/>, replacing those applied there and leaving <paramref name="from"/> without any.
    /// A formula format moved as a whole carries its anchor along, so each cell reads the same cells it did. One
    /// only partly moved keeps a single formula, so its cells that stay keep reading the same cells and the moved
    /// ones read relative references from where they land, as for a copy.
    /// </summary>
    /// <returns>The restore data of each step, to be restored in reverse.</returns>
    internal List<ConditionalFormatRestoreData> MoveImpl(IRegion from, IRegion to)
    {
        var dr = to.Top - from.Top;
        var dc = to.Left - from.Left;

        var formulaFormats = FormulaFormats.ToList();
        var formulas = formulaFormats.Select(f => (f, f.FormulaText)).ToList();
        var movedWhole = formulaFormats
            .Where(f => _appliedFormats.GetRegions(f).All(from.Contains))
            .ToHashSet();

        var pieces = _appliedFormats.GetDataRegions(from)
            .Select(x => (x.Data, Region: x.Region.GetIntersection(from)!))
            .ToList();

        var steps = new List<ConditionalFormatRestoreData>
        {
            new() { Regions = _appliedFormats.Clear(from), Formulas = formulas },
            new() { Regions = _appliedFormats.Clear(to) }
        };

        foreach (var (format, region) in pieces)
        {
            region.Shift(dr, dc);
            var clipped = region.GetIntersection(to);
            if (clipped != null)
                steps.Add(new ConditionalFormatRestoreData { Regions = _appliedFormats.Add(clipped, format) });
        }

        foreach (var format in formulaFormats)
            UpdateAnchor(format, rebaseFormula: !movedWhole.Contains(format));

        _footprints.Clear();
        Prepare(steps.SelectMany(x => x.Regions.RegionsAdded.Concat(x.Regions.RegionsRemoved))
            .Select(x => x.Data)
            .Distinct()
            .ToList());
        return steps;
    }

    /// <summary>
    /// Moves the references of the formula formats that lie entirely within <paramref name="region"/> on
    /// <paramref name="regionSheetName"/>, as when those cells are moved by <paramref name="dRow"/>,
    /// <paramref name="dCol"/> onto <paramref name="destinationSheetName"/>. A relative reference is only moved
    /// when every cell it reads, across the cells the format is applied to, is in the region.
    /// </summary>
    /// <returns>The restore data, or null when no formula changed.</returns>
    internal ConditionalFormatRestoreData? MoveReferencesWithin(IRegion region, string regionSheetName, int dRow,
        int dCol, string destinationSheetName)
    {
        var formulas = new List<(FormulaConditionalFormat, string)>();
        foreach (var format in FormulaFormats)
        {
            var text = format.FormulaText;
            var bounds = GetBounds(format);
            if (bounds == null)
                continue;
            if (!format.MoveReferencesWithin(region, regionSheetName, dRow, dCol, destinationSheetName, _sheet,
                    reference => region.Contains(GetReadRegion(reference, bounds))))
                continue;
            formulas.Add((format, text));
            _sheet.MarkDirty(_appliedFormats.GetRegions(format));
        }

        if (formulas.Count == 0)
            return null;

        _footprints.Clear();
        return new ConditionalFormatRestoreData { Formulas = formulas };
    }

    internal void Restore(ConditionalFormatRestoreData data)
    {
        _appliedFormats.Restore(data.Regions);

        for (var index = data.SplitRegions.Count - 1; index >= 0; index--)
            _appliedFormats.Restore(data.SplitRegions[index]);
        foreach (var format in data.SplitFormats)
            _registered.Remove(format);

        foreach (var (format, formulaText) in data.Formulas)
        {
            format.SetFormulaText(formulaText, _sheet);
            _sheet.MarkDirty(_appliedFormats.GetRegions(format));
        }

        var cfsAffected = data.Regions.RegionsAdded
            .Select(x => x.Data)
            .Concat(data.Regions.RegionsRemoved.Select(x => x.Data));

        foreach (var shift in data.Regions.Shifts ?? Enumerable.Empty<AppliedShift>())
        {
            if (shift.Axis == Axis.Col)
                cfsAffected = cfsAffected.Concat(_appliedFormats.GetData(new ColumnRegion(shift.Index, int.MaxValue)));
            else
                cfsAffected = cfsAffected.Concat(_appliedFormats.GetData(new RowRegion(shift.Index, int.MaxValue)));
        }

        UpdateAnchors();
        _footprints.Clear();
        Prepare(cfsAffected.Distinct().ToList());
    }
}
