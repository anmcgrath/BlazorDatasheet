using System.Linq.Expressions;
using System.Reflection.Metadata;
using BlazorDatasheet.Data;
using BlazorDatasheet.Render;
using BlazorDatasheet.Util;
using Range = BlazorDatasheet.Data.Range;

namespace BlazorDatasheet.ObjectEditor;

public class ObjectEditorBuilder<T>
{
    private List<T> _items;
    private readonly GridDirection _direction;
    private bool _autoGenerateProperties;
    private List<Tuple<string, Action<ObjectPropertyDefinition<T>>>> _propertyActions;
    private List<ObjectPropertyDefinition<T>> _propertyDefinitions;
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
        _suppliedPropertyNames = new List<string>();
    }

    /// <summary>
    /// Set true if the object editor builder should automatically create rows or columns for all public properties. Default is false.
    /// </summary>
    /// <param name="autoGenerateProperties"></param>
    /// <returns></returns>
    public ObjectEditorBuilder<T> AutogenerateProperties(bool autoGenerateProperties = true)
    {
        _autoGenerateProperties = autoGenerateProperties;
        return this;
    }

    /// <summary>
    /// Creates a cell based on the property definition
    /// </summary>
    /// <param name="propDefn"></param>
    /// <returns></returns>
    private Cell GetCell(T item, ObjectPropertyDefinition<T> propDefn)
    {
        var cell = new Cell(item);
        cell.Key = propDefn.PropertyName;
        cell.Formatting = propDefn.Format;
        cell.Type = propDefn.Type;
        cell.IsReadOnly = propDefn.IsReadOnly;
        foreach (var validator in propDefn.Validators)
            cell.Validators.Add(validator);

        return cell;
    }

    public List<Cell> GetCells(T item)
    {
        var cells = new List<Cell>();
        foreach (var defn in _propertyDefinitions)
        {
            cells.Add(GetCell(item, defn));
        }

        return cells;
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
        _propertyDefinitions = new List<ObjectPropertyDefinition<T>>();
        foreach (var propName in distinctPropNames)
        {
            var propType = typeof(T).GetProperty(propName).PropertyType;
            _propertyDefinitions.Add(new ObjectPropertyDefinition<T>(propName, getCellType(propType)));
        }

        var nRows = 0;
        var nCols = 0;

        if (_direction == GridDirection.PropertiesAcrossRows)
        {
            nRows = _propertyDefinitions.Count;
            nCols = _items.Count();
        }
        else if (_direction == GridDirection.PropertiesAcrossColumns)
        {
            nRows = _items.Count();
            nCols = _propertyDefinitions.Count;
        }

        // Run any custom settings on the property definitions
        // I.e those that are defined via WithProperty
        foreach (var actionPair in _propertyActions)
        {
            var propDefinition = _propertyDefinitions.FirstOrDefault(x => x.PropertyName == actionPair.Item1);
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
                T item;
                // Select the correct property definition based on the property direction
                if (_direction == GridDirection.PropertiesAcrossColumns)
                {
                    propDefn = _propertyDefinitions[col];
                    item = _items[row];
                }
                else
                {
                    propDefn = _propertyDefinitions[row];
                    item = _items[col];
                }


                cell = GetCell(item, propDefn);
                cells[row, col] = cell;
            }
        }

        var sheet = new Sheet(nRows, nCols, cells);

        for (int i = 0; i < _propertyDefinitions.Count; i++)
        {
            var propDefn = _propertyDefinitions[i];
            var headings = _direction == GridDirection.PropertiesAcrossColumns
                ? sheet.ColumnHeadings
                : sheet.RowHeadings;
            headings.Add(new Heading()
            {
                Header = string.IsNullOrEmpty(propDefn.Heading)
                    ? propDefn.PropertyName
                    : propDefn.Heading,
            });

            // Apply conditional format to property (whole row or column)
            var conditionalFormats = propDefn.ConditionalFormats;
            foreach (var conditionalFormat in conditionalFormats)
            {
                conditionalFormat.IsShared = true;
                if (_direction == GridDirection.PropertiesAcrossColumns)
                    sheet.ConditionalFormatting.Apply(conditionalFormat, new Range(0, nRows - 1, i, i));
                else if (_direction == GridDirection.PropertiesAcrossRows)
                    sheet.ConditionalFormatting.Apply(conditionalFormat, new Range(i, i, 0, nCols - 1));
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