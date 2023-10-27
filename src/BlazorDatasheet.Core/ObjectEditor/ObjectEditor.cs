using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.ObjectEditor;

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
        Sheet.Rows.InsertRowAt(rowIndex, Sheet);
        for (int i = 0; i < cells.Count; i++)
        {
            Sheet.Cells.SetCell(rowIndex + 1, i, cells[i]);
        }
    }
}