using System.Linq.Expressions;
using System.Reflection.Metadata;
using BlazorDatasheet.Model;
using BlazorDatasheet.Util;
using Range = BlazorDatasheet.Model.Range;

namespace BlazorDatasheet.ObjectEditor;

public class ObjectEditorBuilder<T>
{
    private List<T> _items;
    private readonly GridDirection _direction;
    private bool _autoGenerateProperties;
    private List<Tuple<string, Action<ObjectPropertyDefinition<T>>>> _propertyActions;
    private Dictionary<string, ConditionalFormat> _conditionalFormats;
    private List<string> _suppliedPropertyNames { get; set; }

    public ObjectEditorBuilder(IEnumerable<T> Items, GridDirection direction = GridDirection.PropertiesAcrossColumns)
    {
        _items = Items.ToList();
        _direction = direction;
        _propertyActions = new List<Tuple<string, Action<ObjectPropertyDefinition<T>>>>();
        _conditionalFormats = new Dictionary<string, ConditionalFormat>();
        _suppliedPropertyNames = new List<string>();
    }

    public ObjectEditorBuilder<T> AutogenerateProperties(bool autoGenerateProperties)
    {
        _autoGenerateProperties = autoGenerateProperties;
        return this;
    }

    public ObjectEditorBuilder<T> WithConditionalFormat(string key, ConditionalFormat format)
    {
        _conditionalFormats[key] = format;
        return this;
    }

    public ObjectEditor<T> Build()
    {
        List<string> propertyNames = new List<string>();
        if (_autoGenerateProperties)
            propertyNames.AddRange(autoGenerateProperties());
        
        propertyNames.AddRange(_suppliedPropertyNames);
        var distinctPropNames = propertyNames.Distinct();

        List<ObjectPropertyDefinition<T>> propertyDefinitions = new List<ObjectPropertyDefinition<T>>();
        foreach (var propName in distinctPropNames)
        {
            var propType = typeof(T).GetProperty(propName).PropertyType;
            propertyDefinitions.Add(new ObjectPropertyDefinition<T>(propName, getCellType(propType)));
        }

        var nRows = 0;
        var nCols = 0;

        if (_direction == GridDirection.PropertiesAcrossRows)
        {
            nRows = propertyDefinitions.Count;
            nCols = _items.Count();
        }
        else if (_direction == GridDirection.PropertiesAcrossColumns)
        {
            nRows = _items.Count();
            nCols = propertyDefinitions.Count;
        }

        // Run any custom settings on the property definitions
        foreach (var actionPair in _propertyActions)
        {
            var propDefinition = propertyDefinitions.FirstOrDefault(x => x.PropertyName == actionPair.Item1);
            if (propDefinition == null)
                continue;

            actionPair.Item2.Invoke(propDefinition);
        }

        var cells = new Cell[nRows, nCols];
        for (int row = 0; row < nRows; row++)
        {
            for (int col = 0; col < nCols; col++)
            {
                var cell = new Cell();
                ObjectPropertyDefinition<T> propDefn;
                if (_direction == GridDirection.PropertiesAcrossColumns)
                {
                    propDefn = propertyDefinitions[col];
                    cell.Data = _items[row];
                }
                else
                {
                    propDefn = propertyDefinitions[row];
                    cell.Data = _items[col];
                }

                cell.Key = propDefn.PropertyName;

                cell.Setter = propDefn.SetterObj;
                cell.Formatting = propDefn.Format;
                cell.Type = propDefn.Type;
                cells[row, col] = cell;
            }
        }

        var sheet = new Sheet(nRows, nCols, cells);

        // Add conditional formats
        foreach (var cf in _conditionalFormats)
            sheet.RegisterConditionalFormat(cf.Key, cf.Value);

        for (int i = 0; i < propertyDefinitions.Count; i++)
        {
            var propDefn = propertyDefinitions[i];
            var headings = _direction == GridDirection.PropertiesAcrossColumns
                ? sheet.ColumnHeadings
                : sheet.RowHeadings;
            headings.Add(new Heading()
            {
                Header = String.IsNullOrEmpty(propDefn.Heading)
                    ? propDefn.PropertyName
                    : propDefn.Heading,
            });

            // Apply conditional format to property (whole row or column)
            var cfs = propDefn.ConditionalFormatKeys;
            foreach (var cf in cfs)
            {
                if (_direction == GridDirection.PropertiesAcrossColumns)
                    sheet.ApplyConditionalFormat(cf, new Range(0, nRows, i, i));
                else if (_direction == GridDirection.PropertiesAcrossRows)
                    sheet.ApplyConditionalFormat(cf, new Range(i, i, 0, nCols));
            }
        }

        var objectEditor = new ObjectEditor<T>(sheet, this);
        return objectEditor;
    }

    public ObjectEditorBuilder<T> WithProperty<TProperty>(Expression<Func<T, TProperty>> propertySelector,
        Action<ObjectPropertyDefinition<T>> action)
    {
        var propName = Properties.GetPropertyInfo(propertySelector).Name;
        _propertyActions.Add(new Tuple<string, Action<ObjectPropertyDefinition<T>>>(propName, action));
        _suppliedPropertyNames.Add(propName);

        return this;
    }

    private IEnumerable<string> autoGenerateProperties()
    {
        var propInfos = typeof(T).GetProperties();
        return propInfos.Select(x => x.Name);
    }

    private string getCellType(Type type)
    {
        if (type.IsNullable())
            type = Nullable.GetUnderlyingType(type);

        if (type.IsNumeric())
            return "number";

        if (type == typeof(bool))
            return "boolean";

        if (type == typeof(DateTime))
            return "datetime";

        return "text";
    }
}