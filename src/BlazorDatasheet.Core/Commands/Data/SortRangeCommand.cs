using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Metadata;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Commands.Data;

public class SortRangeCommand : BaseCommand, IUndoableCommand
{
    private readonly IRegion _region;
    public IRegion? SortedRegion;
    private readonly List<ColumnSortOptions> _sortOptions;
    public int[] OldIndices = Array.Empty<int>();
    private RegionRestoreData<string> _typeRestoreData = new();
    private RegionRestoreData<CellMetadata> _metaDataRestoreData = new();
    private RegionRestoreData<int> _validatorRestoreData = new();

    /// <summary>
    /// Sorts the specified region on values using the specified sort options.
    /// </summary>
    /// <param name="region">The region to sort</param>
    /// <param name="sortOptions">The column sort options, if null the default sort (sort on column 0 ascending) will be used.
    /// If two column values are equal, then the next option will be used for the sort, equivalent to a ThenBy</param>
    public SortRangeCommand(IRegion region, List<ColumnSortOptions>? sortOptions = null)
    {
        _region = region;
        _sortOptions = sortOptions ?? [new(0, true)];
    }

    /// <summary>
    /// Sorts the specified region on values using the specified sort options.
    /// </summary>
    /// <param name="region">The region to sort</param>
    /// <param name="sortOption">The column sort options, if null the default sort (sort on column 0 ascending) will be used.
    /// If two column values are equal, then the next option will be used for the sort, equivalent to a ThenBy</param>
    public SortRangeCommand(IRegion region, ColumnSortOptions sortOption)
    {
        _region = region;
        _sortOptions = new List<ColumnSortOptions> { sortOption };
    }

    public override bool CanExecute(Sheet sheet) => true;

    public override bool Execute(Sheet sheet)
    {
        var store = sheet.Cells.GetCellDataStore();


        var sortRegion = _region.GetIntersection(sheet.Region);
        if (sortRegion == null)
            return true;

        var rowCollection = store.GetNonEmptyRowData(_region);

        var participating = GetParticipatingRows(sheet, sortRegion, rowCollection);
        var rowIndices = new Span<int>(participating.RowIndicies);
        var rowData = new Span<RowData<CellValue>>(participating.Rows);

        if (rowIndices.Length == 0)
            return true;

        rowData.Sort(rowIndices, Comparison);

        SortedRegion = new Region(_region.Top, _region.Top + rowIndices.Length - 1, _region.Left, _region.Right);
        SortedRegion = SortedRegion.GetIntersection(sheet.Region);

        if (SortedRegion == null)
            return true;

        sheet.BatchUpdates();

        var formulaData = sheet.Cells.GetFormulaStore().GetSubStore(_region, false);
        var typeData = sheet.Cells.GetTypeStore().GetSubStore(_region, false);
        var metaDataCollection = sheet.Cells.GetMetaDataStore().GetSubStore(_region, false);
        var validData = sheet.Validators.Store.GetSubStore(_region, false);

        var affectedRegions = GetAffectedRegions(rowData, SortedRegion);
        sheet.Cells.ClearCellsImpl(affectedRegions);
        // A command instance is executed again on redo, so each execution needs a fresh undo snapshot.
        _typeRestoreData = ClearRegions(sheet.Cells.GetTypeStore(), affectedRegions);
        _metaDataRestoreData = ClearRegions(sheet.Cells.GetMetaDataStore(), affectedRegions);
        _validatorRestoreData = ClearRegions(sheet.Validators.Store, affectedRegions);

        for (int i = 0; i < rowData.Length; i++)
        {
            var newRowNo = _region.Top + i;
            var oldRowNo = rowData[i].Row;

            for (int j = 0; j < rowData[i].ColumnIndices.Length; j++)
            {
                var col = rowData[i].ColumnIndices[j];
                var val = rowData[i].Values[j];
                var formula = formulaData.Get(oldRowNo, col);

                if (formula == null)
                    sheet.Cells.SetValueImpl(newRowNo, col, val);
                else
                {
                    formula.ShiftReferences((newRowNo - oldRowNo), 0, sheet.Name);
                    sheet.Cells.SetFormulaImpl(newRowNo, col, formula);
                }
            }

            MoveRowState(sheet, sortRegion, oldRowNo, newRowNo, typeData, metaDataCollection, validData);
        }

        // Revalidate once all validators are in their final positions so the cached validity follows the sorted cells.
        sheet.Cells.ValidateRegion(SortedRegion);

        sheet.EndBatchUpdates();

        OldIndices = rowIndices.ToArray();

        return true;
    }

    private int Comparison(RowData<CellValue> x, RowData<CellValue> y)
    {
        for (int i = 0; i < _sortOptions.Count; i++)
        {
            var sortOption = _sortOptions[i];
            var xValue = x.GetColumnData(sortOption.ColumnIndex + _region.Left);
            var yValue = y.GetColumnData(sortOption.ColumnIndex + _region.Left);

            if (xValue.IsEmpty && yValue.IsEmpty)
                continue;

            // null comparisons shouldn't depend on the sort order -
            // null values always end up last.
            if (xValue.IsEmpty)
                return 1;
            if (yValue.IsEmpty)
                return -1;

            int comparison = xValue.CompareTo(yValue);

            if (comparison == 0)
                continue;

            comparison = sortOption.Ascending ? comparison : -comparison;

            return comparison;
        }

        return 0;
    }

    public bool Undo(Sheet sheet)
    {
        if (SortedRegion == null)
            return true;

        var rowCollection = sheet.Cells.GetCellDataStore().GetRowData(SortedRegion);
        var formulaCollection = sheet.Cells.GetFormulaStore().GetSubStore(SortedRegion, false);
        var rowData = rowCollection.Rows;
        var rowIndices = rowCollection.RowIndicies;

        sheet.BatchUpdates();
        var affectedRegions = GetAffectedRegions(rowData, SortedRegion);
        sheet.Cells.ClearCellsImpl(affectedRegions);
        ClearRegions(sheet.Validators.Store, affectedRegions);
        ClearRegions(sheet.Cells.GetTypeStore(), affectedRegions);
        ClearRegions(sheet.Cells.GetMetaDataStore(), affectedRegions);

        for (int i = 0; i < OldIndices.Length; i++)
        {
            var newRowNo = OldIndices[i];
            for (int j = 0; j < rowData[i].ColumnIndices.Length; j++)
            {
                var col = rowData[i].ColumnIndices[j];
                var formula = formulaCollection.Get(rowIndices[i], col);

                if (formula == null)
                {
                    var val = rowData[i].Values[j];
                    sheet.Cells.SetValueImpl(newRowNo, col, val);
                }
                else
                {
                    formula.ShiftReferences((newRowNo - rowIndices[i]), 0, sheet.Name);
                    sheet.Cells.SetFormulaImpl(newRowNo, col, formula);
                }
            }
        }

        var restoreData = new CellStoreRestoreData()
        {
            TypeRestoreData = _typeRestoreData
        };

        sheet.Cells.Restore(restoreData);
        sheet.Cells.GetMetaDataStore().Restore(_metaDataRestoreData);
        sheet.Validators.Store.Restore(_validatorRestoreData);
        sheet.Cells.ValidateRegion(SortedRegion);
        foreach (var row in OldIndices.Distinct())
        {
            if (row < SortedRegion.Top || row > SortedRegion.Bottom)
                sheet.Cells.ValidateRegion(new Region(row, row, SortedRegion.Left, SortedRegion.Right));
        }

        sheet.EndBatchUpdates();

        return true;
    }

    private List<IRegion> GetAffectedRegions(Span<RowData<CellValue>> rowData, IRegion sortedRegion)
    {
        return GetAffectedRegions(rowData.ToArray(), sortedRegion);
    }

    private static List<IRegion> GetAffectedRegions(RowData<CellValue>[] rowData, IRegion sortedRegion)
    {
        var affectedRegions = new List<IRegion> { sortedRegion };
        for (int i = 0; i < rowData.Length; i++)
        {
            var row = rowData[i].Row;
            if (row >= sortedRegion.Top && row <= sortedRegion.Bottom)
                continue;

            affectedRegions.Add(new Region(row, row, sortedRegion.Left, sortedRegion.Right));
        }

        return affectedRegions;
    }

    private (int[] RowIndicies, RowData<CellValue>[] Rows) GetParticipatingRows(
        Sheet sheet,
        IRegion sortRegion,
        RowDataCollection<CellValue> valueRows)
    {
        var stateRows = new HashSet<int>();
        CollectRowsWithData(sheet.Cells.GetTypeStore(), sortRegion, stateRows);
        CollectRowsWithData(sheet.Cells.GetMetaDataStore(), sortRegion, stateRows);
        CollectRowsWithData(sheet.Validators.Store, sortRegion, stateRows);

        // rows that already hold a value are covered by valueRows
        foreach (var row in valueRows.RowIndicies)
            stateRows.Remove(row);

        if (stateRows.Count == 0)
            return (valueRows.RowIndicies, valueRows.Rows);

        var extraRows = stateRows.ToList();
        extraRows.Sort();

        var indices = new List<int>(valueRows.RowIndicies);
        var rows = new List<RowData<CellValue>>(valueRows.Rows);

        foreach (var row in extraRows)
        {
            indices.Add(row);
            rows.Add(new RowData<CellValue>(row, Array.Empty<int>(), Array.Empty<CellValue>()));
        }

        return (indices.ToArray(), rows.ToArray());
    }

    private static void CollectRowsWithData<T>(RegionDataStore<T> store, IRegion sortRegion, HashSet<int> rows)
        where T : IEquatable<T>
    {
        foreach (var dataRegion in store.GetDataRegions(sortRegion))
        {
            var intersection = dataRegion.Region.GetIntersection(sortRegion);
            if (intersection == null)
                continue;

            if (intersection.Top == sortRegion.Top && intersection.Bottom == sortRegion.Bottom)
                continue;

            for (int row = intersection.Top; row <= intersection.Bottom; row++)
                rows.Add(row);
        }
    }

    private static void MoveRowState(
        Sheet sheet,
        IRegion sortRegion,
        int fromRow,
        int toRow,
        RegionDataStore<string> typeData,
        RegionDataStore<CellMetadata> metaData,
        RegionDataStore<int> validatorData)
    {
        var fromSlice = new Region(fromRow, fromRow, sortRegion.Left, sortRegion.Right);

        foreach (var (region, type) in GetRowSpans(typeData, fromSlice))
            sheet.Cells.SetCellTypeImpl(new Region(toRow, toRow, region.Left, region.Right), type);

        foreach (var (region, validatorIndex) in GetRowSpans(validatorData, fromSlice))
            sheet.Validators.AddImpl(validatorIndex, new Region(toRow, toRow, region.Left, region.Right));

        foreach (var (region, cellMetaData) in GetRowSpans(metaData, fromSlice))
        {
            foreach (var kp in cellMetaData.GetItems())
            {
                for (int col = region.Left; col <= region.Right; col++)
                    sheet.Cells.SetMetaDataImpl(toRow, col, kp.Key, kp.Value);
            }
        }
    }

    /// <summary>
    /// The column spans of <paramref name="store"/> that overlap a single-row slice, clipped to it.
    /// </summary>
    private static IEnumerable<(IRegion Region, T Data)> GetRowSpans<T>(RegionDataStore<T> store, IRegion rowSlice)
        where T : IEquatable<T>
    {
        foreach (var dataRegion in store.GetDataRegions(rowSlice))
        {
            var intersection = dataRegion.Region.GetIntersection(rowSlice);
            if (intersection != null)
                yield return (intersection, dataRegion.Data);
        }
    }

    private static RegionRestoreData<T> ClearRegions<T>(RegionDataStore<T> store, IEnumerable<IRegion> regions)
        where T : IEquatable<T>
    {
        var restoreData = new RegionRestoreData<T>();
        foreach (var region in regions)
            restoreData.Merge(store.Clear(region));

        return restoreData;
    }
}

public class ColumnSortOptions
{
    /// <summary>
    /// The column index, relative to the range being sorted.
    /// </summary>
    public int ColumnIndex { get; set; }

    /// <summary>
    /// Whether to sort in ascending order.
    /// </summary>
    public bool Ascending { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="columnIndex"></param>
    /// <param name="ascending"></param>
    public ColumnSortOptions(int columnIndex, bool ascending)
    {
        ColumnIndex = columnIndex;
        Ascending = ascending;
    }
}