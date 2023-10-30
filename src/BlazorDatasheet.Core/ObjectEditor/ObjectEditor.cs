using System.Collections;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.ObjectEditor;

public class ObjectEditor<T>
{
    private readonly IQueryable<T> _dataSource;
    private readonly Func<int, T, object> _valueColumnSelector;
    private readonly ObjectEditorBuilder<T> _builder;

    public int PageSize { get; private set; }
    public int NumPages { get; private set; }
    public int CurrentPage { get; private set; }
    public int NColumns { get; private set; }
    public Sheet Sheet { get; }

    private const string ItemMetaDataName = "obj";

    internal ObjectEditor(Sheet sheet, IQueryable<T> dataSource, Func<int, T, object> valueColumnSelector, int nColumns)
    {
        _dataSource = dataSource;
        _valueColumnSelector = valueColumnSelector;
        NColumns = nColumns;
        Sheet = sheet;

        Sheet.CellsChanged += SheetOnCellsChanged;

        SetPageSize(10);
    }

    private void SheetOnCellsChanged(object? sender, IEnumerable<CellPosition> e)
    {
        foreach (var pos in e)
        {
            var item = (T?)Sheet.Cells.GetMetaData(pos.row, pos.col, ItemMetaDataName);
            if (item != null)
            {
                
            }
        }
    }

    public void SetPageSize(int nPages)
    {
        PageSize = nPages;
        NumPages = _dataSource.Count() / PageSize;
        if (Sheet.NumRows < PageSize)
            Sheet.Rows.InsertRowAt(0, PageSize - Sheet.NumRows);
        if (Sheet.NumRows > PageSize)
            Sheet.Rows.RemoveAt(0, Sheet.NumRows - PageSize);
        RefreshView();
    }

    public void SetPage(int n)
    {
        CurrentPage = Math.Max(0, Math.Min(NumPages - 1, n));
        RefreshView();
    }

    private void RefreshView()
    {
        Sheet.BatchUpdates();
        Sheet.Range(Sheet.Region).Clear();

        var values = new List<(int row, int col, object value)>();
        var items = _dataSource.Skip(PageSize * CurrentPage).Take(PageSize).ToList();

        for (int i = 0; i < items.Count; i++)
        {
            for (int j = 0; j < NColumns; j++)
            {
                var data = _valueColumnSelector(j, items[i]);
                values.Add((i, j, data));
                Sheet.Cells.SetCellMetaData(i, j, ItemMetaDataName, items[i]);
            }
        }

        Sheet.Cells.SetValues(values);
        Sheet.EndBatchUpdates();
        Sheet.Commands.ClearHistory();
    }
}