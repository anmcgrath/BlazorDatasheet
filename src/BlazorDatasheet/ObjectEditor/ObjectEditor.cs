using BlazorDatasheet.Model;

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
}