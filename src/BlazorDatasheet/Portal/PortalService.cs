using Microsoft.AspNetCore.Components;

namespace BlazorDatasheet.Portal;

public class PortalService
{
    private readonly Dictionary<(string id, string dataSheetId), List<RenderFragment>> _portalTargets = new();
    private readonly Dictionary<(string id, string dataSheetId), PortalTarget> _portalTargetComponents = new();

    public void RegisterPortalTarget(string id, string datasheetId, PortalTarget target)
    {
        _portalTargets.TryAdd((id, datasheetId), new List<RenderFragment>());
        _portalTargetComponents.TryAdd((id, datasheetId), target);
    }

    public void AddToPortal(string portalTargetId, string datasheetId, RenderFragment fragment)
    {
        if (_portalTargets.TryGetValue((portalTargetId, datasheetId), out var fragments))
        {
            fragments.Add(fragment);

            if (_portalTargetComponents.TryGetValue((portalTargetId, datasheetId), out var target))
                target.SetRenderFragments(fragments);
        }
    }

    public IEnumerable<RenderFragment> GetFragments(string portalTargetId, string datasheetId)
    {
        return _portalTargets.TryGetValue((portalTargetId, datasheetId), out var fragments)
            ? fragments
            : Enumerable.Empty<RenderFragment>();
    }

    public void RenderPortalTarget(string id, string datasheetId)
    {
        if (_portalTargetComponents.TryGetValue((id, datasheetId), out var target))
            target.Render();
    }
}