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

    /// <summary>
    /// An ObjectEditorBuilder creates an ObjectEditor & Sheet consisting of the Items
    /// </summary>
    /// <param name="Items">The Items in the ObjectEditor sheet</param>
    /// <param name="direction">The direction that the object properties are displayed, either across the Datasheet columns or rows.</param>
    public ObjectEditorBuilder(IEnumerable<T> Items, GridDirection direction = GridDirection.PropertiesAcrossColumns)
    {
        _items = Items.ToList();
        _direction = direction;
        _propertyActions = new List<Tuple<string, Action<ObjectPropertyDefinition<T>>>>();
        _conditionalFormats = new Dictionary<string, ConditionalFormat>();
        _suppliedPropertyNames = new List<string>();
    }

    /// <summary>
    /// Set true if the object editor builder should automatically create rows or columns for all public properties.
    /// </summary>
    /// <param name="autoGenerateProperties"></param>
    /// <returns></returns>
    public ObjectEditorBuilder<T> AutogenerateProperties(bool autoGenerateProperties)
    {
        _autoGenerateProperties = autoGenerateProperties;
        return this;
    }

    /// <summary>
    /// Register a conditional format with the the Datasheet
    /// </summary>
    /// <param name="key">The unique key of the conditional format</param>
    /// <param name="format">The conditional format to apply</param>
    /// <returns></returns>
    public ObjectEditorBuilder<T> WithConditionalFormat(string key, ConditionalFormat format)
    {
        _conditionalFormats[key] = format;
        return this;
    }

    /// <summary>
    /// Create the ObjectEditor
    /// </summary>
    /// <returns></returns>
    public ObjectEditor<T> Build()
    {
        List<string> propertyNames = new List<string>();
        if (_autoGenerateProperties)
            propertyNames.AddRange(autoGenerateProperties());

        // We don't want to have the same property twice, so
        // we only take the unique set of names from auto-generated
        // and supplied property names
        propertyNames.AddRange(_suppliedPropertyNames);
        var distinctPropNames = propertyNames.Distinct();

        // Create default property definitions for each property
        // we override the defaults later with user-defined options
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
        // I.e those that are defined via WithProperty
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
                Cell cell;
                ObjectPropertyDefinition<T> propDefn;
                if (_direction == GridDirection.PropertiesAcrossColumns)
                {
                    propDefn = propertyDefinitions[col];
                    cell = new Cell(_items[row]);
                }
                else
                {
                    propDefn = propertyDefinitions[row];
                    cell = new Cell(_items[col]);
                }

                cell.Key = propDefn.PropertyName;
                cell.Formatting = propDefn.Format;
                cell.Type = propDefn.Type;
                cell.IsReadOnly = propDefn.IsReadOnly;
                foreach (var validator in propDefn.Validators)
                    cell.Validators.Add(validator);
                cells[row, col] = cell;
            }
        }

        var sheet = new Sheet(nRows, nCols, cells);

        // Add conditional formats (1. Register conditional format 2. apply it to the correct cells)
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
            var conditionalFormatKeys = propDefn.ConditionalFormatKeys;
            foreach (var key in conditionalFormatKeys)
            {
                if (_direction == GridDirection.PropertiesAcrossColumns)
                    sheet.ApplyConditionalFormat(key, new Range(0, nRows, i, i));
                else if (_direction == GridDirection.PropertiesAcrossRows)
                    sheet.ApplyConditionalFormat(key, new Range(i, i, 0, nCols));
            }
        }

        var objectEditor = new ObjectEditor<T>(sheet, this);
        return objectEditor;
    }

    /// <summary>
    /// Include a property in the Object Editor
    /// </summary>
    /// <param name="propertySelector">The property name</param>
    /// <param name="propDefinitionFunc">Apply options to a property definition</param>
    /// <typeparam name="TProperty"></typeparam>
    /// <returns></returns>
    public ObjectEditorBuilder<T> WithProperty<TProperty>(Expression<Func<T, TProperty>> propertySelector,
        Action<ObjectPropertyDefinition<T>> propDefinitionFunc)
    {
        var propName = Properties.GetPropertyInfo(propertySelector).Name;
        _propertyActions.Add(new Tuple<string, Action<ObjectPropertyDefinition<T>>>(propName, propDefinitionFunc));
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