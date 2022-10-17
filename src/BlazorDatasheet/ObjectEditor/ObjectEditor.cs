using BlazorDatasheet.Data;

namespace BlazorDatasheet.ObjectEditor;

public class ObjectEditor<T>
{
    private readonly ObjectEditorBuilder<T> _builder;
    public Sheet Sheet { get; private set; }

    internal ObjectEditor(Sheet sheet, ObjectEditorBuilder<T> builder)
    {
        Sheet = sheet;
        _builder = builder;
    }

    public void InsertAt(T item, int index)
    {
        var cells = _builder.GetCells(item);
        var row = new Row(cells, -1);
        Sheet.InsertRowAt(index, row);
    }
}