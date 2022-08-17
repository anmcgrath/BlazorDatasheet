using System.Linq.Expressions;
using BlazorDatasheet.Model;
using BlazorDatasheet.Util;

namespace BlazorDatasheet.ObjectEditor;

public class ObjectEditorBuilder<T>
{
    private List<T> _items;
    private readonly GridDirection _direction;
    private bool _autoGenerateProperties;
    private List<Tuple<string, Action<ObjectPropertyDefinition<T>>>> _propertyActions;

    public ObjectEditorBuilder(IEnumerable<T> Items, GridDirection direction = GridDirection.PropertiesAcrossColumns)
    {
        _items = Items.ToList();
        _direction = direction;
        _propertyActions = new List<Tuple<string, Action<ObjectPropertyDefinition<T>>>>();
    }

    public ObjectEditorBuilder<T> AutogenerateProperties(bool autoGenerateProperties)
    {
        _autoGenerateProperties = autoGenerateProperties;
        return this;
    }

    public ObjectEditor<T> Build()
    {
        List<ObjectPropertyDefinition<T>> propertyDefinitions = new List<ObjectPropertyDefinition<T>>();
        if (_autoGenerateProperties)
            propertyDefinitions.AddRange(autoGenerateProperties());

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
            var propDefinition = propertyDefinitions.FirstOrDefault(x => x.Key == actionPair.Item1);
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

                cell.Key = propDefn.Key;

                cell.Setter = propDefn.SetterObj;
                cell.Formatting = propDefn.Format;
                cell.Type = propDefn.Type;
                cells[row, col] = cell;
            }
        }

        var sheet = new Sheet(nRows, nCols, cells);

        for (int i = 0; i < propertyDefinitions.Count; i++)
        {
            var headings = _direction == GridDirection.PropertiesAcrossColumns
                ? sheet.ColumnHeadings
                : sheet.RowHeadings;
            headings.Add(new Heading()
            {
                Header = String.IsNullOrEmpty(propertyDefinitions[i].Heading)
                    ? propertyDefinitions[i].Key
                    : propertyDefinitions[i].Heading,
            });
        }

        var objectEditor = new ObjectEditor<T>(sheet, this);
        return objectEditor;
    }

    public ObjectEditorBuilder<T> WithProperty<TProperty>(Expression<Func<T, TProperty>> propertySelector,
        Action<ObjectPropertyDefinition<T>> action)
    {
        var propName = Properties.GetPropertyInfo(propertySelector).Name;
        _propertyActions.Add(new Tuple<string, Action<ObjectPropertyDefinition<T>>>(propName, action));

        return this;
    }

    private IEnumerable<ObjectPropertyDefinition<T>> autoGenerateProperties()
    {
        var objProperties = new List<ObjectPropertyDefinition<T>>();
        var propInfos = typeof(T).GetProperties();
        foreach (var propInfo in propInfos)
        {
            objProperties.Add(new ObjectPropertyDefinition<T>()
            {
                Key = propInfo.Name,
                Type = getCellType(propInfo.PropertyType)
            });
        }

        return objProperties;
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