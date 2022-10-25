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

    public void InsertAt(T item, int rowIndex)
    {
        var cells = _builder.GetCells(item);
        Sheet.InsertRowAt(rowIndex);
        for (int i = 0; i < cells.Count; i++)
        {
            Sheet.SetCell(rowIndex, i, cells[i]);
        }
    }
}