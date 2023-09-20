using BlazorDatasheet.Data;

namespace BlazorDatasheet.ObjectEditor;

public class ObjectEditor<T>
{
    private readonly ObjectEditorBuilder<T> _builder;
    public Sheet Sheet { get; }

    internal ObjectEditor(Sheet sheet, ObjectEditorBuilder<T> builder)
    {
        Sheet = sheet;
        _builder = builder;
    }

    public void InsertAt(T item, int rowIndex)
    {
        var cells = _builder.GetCells(item);
        Sheet.InsertRowAfter(rowIndex);
        for (int i = 0; i < cells.Count; i++)
        {
            Sheet.SetCell(rowIndex + 1, i, cells[i]);
        }
    }
}