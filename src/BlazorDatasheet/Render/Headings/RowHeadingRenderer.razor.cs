using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Virtualise;

namespace BlazorDatasheet.Render.Headings;

public partial class RowHeadingRenderer : HeadingRenderer
{
    private Virtualise2D? _topView;
    private Virtualise2D? _bottomView;

    public RowHeadingRenderer() : base(Axis.Row)
    {
    }

    protected override async Task RefreshAdditionalViews()
    {
        if (_topView != null)
            await _topView.RefreshView();

        if (_bottomView != null)
            await _bottomView.RefreshView();
    }
}
