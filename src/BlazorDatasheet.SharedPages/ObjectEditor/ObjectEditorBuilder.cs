using System.Linq.Expressions;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Util;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.SharedPages.ObjectEditor;

namespace BlazorDatasheet.Core.ObjectEditor;

public class ObjectEditorBuilder<T>
{
    private readonly IQueryable<T> _dataSource;
    private List<ObjectPropertyBuilder<T>> _properties = new();
    private Func<T, string> _rowHeadingSelector = x => null;
    private int _pageSize = 10;

    public ObjectEditorBuilder(IQueryable<T> dataSource)
    {
        _dataSource = dataSource;
    }

    public ObjectEditor<T> Build()
    {
        var sheet = new Sheet(_dataSource.Count(), _properties.Count);

        for (int i = 0; i < _properties.Count; i++)
        {
            if (_properties[i].CellFormat != null)
                sheet.SetFormat(new ColumnRegion(i), _properties[i].CellFormat);
            foreach (var cf in _properties[i].ConditionalFormats)
                sheet.ConditionalFormats.Apply(new ColumnRegion(i), cf);
            foreach (var validator in _properties[i].Validators)
                sheet.Validators.Add(new ColumnRegion(i), validator);
            sheet.Cells.SetType(new ColumnRegion(i), _properties[i].Type);
            sheet.Columns.SetHeadings(i, i, _properties[i].Heading ?? _properties[i].PropertyName);
        }

        return new ObjectEditor<T>(sheet,
            _pageSize,
            _dataSource,
            _rowHeadingSelector,
            (i, o) => _properties[i].GetPropertyValue(o)!,
            (i, o, v) => _properties[i].SetPropertyValue(o, v),
            _properties.Count);
    }

    /// <summary>
    /// Include a property in the Object Editor
    /// </summary>
    /// <param name="propertySelector">The property name</param>
    /// <param name="propDefn"></param>
    /// <typeparam name="TProperty"></typeparam>
    /// <returns></returns>
    public ObjectEditorBuilder<T> WithProperty<TProperty>(Expression<Func<T, TProperty>> propertySelector,
        Action<ObjectPropertyBuilder<T>>? propDefn = null)
    {
        var propName = Properties.GetPropertyInfo(propertySelector).Name;
        var propertyBuilder = new ObjectPropertyBuilder<T>(propName);
        if (propDefn != null)
            propDefn.Invoke(propertyBuilder);

        _properties.Add(propertyBuilder);
        return this;
    }

    public ObjectEditorBuilder<T> WithPageSize(int pageSize)
    {
        _pageSize = pageSize;
        return this;
    }
    
    public ObjectEditorBuilder<T> WithRowHeadingSelector(Func<T, string> rowHeadingSelector)
    {
        _rowHeadingSelector = rowHeadingSelector;
        return this;
    }
}