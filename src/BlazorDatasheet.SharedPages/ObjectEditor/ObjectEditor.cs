using System.Collections;
using System.Diagnostics;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Events.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.ObjectEditor;

public class ObjectEditor<T>
{
    private readonly IQueryable<T> _dataSource;
    private readonly Func<T, string> _rowHeadingSelector;
    private readonly Func<int, T, object> _valueColumnSelector;
    private readonly Action<int, T, object> _valueColumnSetter;
    private readonly ObjectEditorBuilder<T> _builder;

    public int PageSize { get; private set; }
    public int NumPages { get; private set; }
    public int CurrentPage { get; private set; }
    public int NColumns { get; private set; }
    public Sheet Sheet { get; }
    public Region? View { get; set; }

    private const string ItemMetaData = "obj";

    internal ObjectEditor(Sheet sheet,
        int pageSize,
        IQueryable<T> dataSource,
        Func<T, string> rowHeadingSelector,
        Func<int, T, object> valueColumnSelector,
        Action<int, T, object> valueColumnSetter,
        int nColumns)
    {
        _dataSource = dataSource;
        _rowHeadingSelector = rowHeadingSelector;
        _valueColumnSelector = valueColumnSelector;
        _valueColumnSetter = valueColumnSetter;
        NColumns = nColumns;
        Sheet = sheet;

        Sheet.Cells.CellsChanged += SheetOnCellsChanged;

        var items = _dataSource.ToList();
        var values = new object[items.Count][];

        for (int i = 0; i < items.Count; i++)
        {
            values[i] = new object[NColumns];
            for (int j = 0; j < NColumns; j++)
            {
                var data = _valueColumnSelector(j, items[i]);
                values[i][j] = data;
                Sheet.Cells.SetCellMetaData(i, j, ItemMetaData, items[i]);
            }
        }

        Sheet.Cells.SetValues(0, 0, values);

        SetPageSize(pageSize);
    }

    private void SheetOnCellsChanged(object? sender, CellDataChangedEventArgs args)
    {
        foreach (var pos in args.Positions)
        {
            var value = Sheet.Cells.GetValue(pos.row, pos.col);
            var item = (T?)Sheet.Cells.GetMetaData(pos.row, pos.col, ItemMetaData);
            if (item != null)
            {
                _valueColumnSetter.Invoke(pos.col, item, value);
            }
        }
    }

    private void SetPageSize(int nPages)
    {
        PageSize = nPages;
        NumPages = _dataSource.Count() / PageSize;
        RefreshView();
    }

    public void SetPage(int n)
    {
        CurrentPage = Math.Max(0, Math.Min(NumPages - 1, n));
        RefreshView();
    }

    private void RefreshView()
    {
        View = new Region(CurrentPage * PageSize, CurrentPage * PageSize + PageSize - 1, 0, NColumns - 1);
        Sheet.Commands.ClearHistory();
    }
}