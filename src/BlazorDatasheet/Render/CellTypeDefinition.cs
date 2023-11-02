using BlazorDatasheet.Core.Interfaces;

namespace BlazorDatasheet.Render;

public class CellTypeDefinition
{
    public Type EditorType { get; }
    public Type RendererType { get; }

    internal CellTypeDefinition(Type editorType, Type rendererType)
    {
        EditorType = editorType;
        RendererType = rendererType;
    }

    public static CellTypeDefinition Create<TEditorType, TRendererType>()
        where TEditorType : ICellEditor
        where TRendererType : BaseRenderer
    {
        return new CellTypeDefinition(typeof(TEditorType), typeof(TRendererType));
    }
}