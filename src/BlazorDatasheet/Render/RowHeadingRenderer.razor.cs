using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Render;

public partial class RowHeadingRenderer : HeadingRenderer
{
    public RowHeadingRenderer() : base(Axis.Row)
    {
    }
}