using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Virtualise;

namespace BlazorDatasheet.Render.Headings;

public partial class ColumnHeadingRenderer : HeadingRenderer
{
    private Virtualise2D? _leftView;
    private Virtualise2D? _rightView;

    public ColumnHeadingRenderer() : base(Axis.Col)
    {
        
    }

    protected override async Task RefreshAdditionalViews()
    {
        if (_leftView != null)
            await _leftView.RefreshView();

        if (_rightView != null)
            await _rightView.RefreshView();
    }
}
