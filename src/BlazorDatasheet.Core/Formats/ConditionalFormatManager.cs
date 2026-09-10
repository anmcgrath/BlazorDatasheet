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
            conditionalFormat.Order = _registered.Count - 1;
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
            var extraRows = bounds.Height - 1;
            var extraCols = bounds.Width - 1;

            foreach (var reference in format.References)
            {
                if (reference.SheetName != _sheet.Name || reference.IsInvalid)
                    continue;

                if (reference.Kind != ReferenceKind.Cell && reference.Kind != ReferenceKind.Range)
                    continue;

                var region = reference.Region.Clone();
                // an unbounded edge already covers everything the shift could reach.
                if (extraRows > 0 && IsRowRelative(reference) && region.Bottom != int.MaxValue)
                    region.Expand(Edge.Bottom, extraRows);
                if (extraCols > 0 && IsColRelative(reference) && region.Right != int.MaxValue)
                    region.Expand(Edge.Right, extraCols);

                footprint.Add(region);
            }
        }

        _footprints[format] = footprint;
        return footprint;
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

        if (!_sheet.Region.Contains(row, col))
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

    internal void Restore(ConditionalFormatRestoreData data)
    {
        _appliedFormats.Restore(data.Regions);

        foreach (var (format, formulaText) in data.Formulas)
            format.SetFormulaText(formulaText, _sheet);

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
