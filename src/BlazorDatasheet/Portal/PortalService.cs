using Microsoft.AspNetCore.Components;

namespace BlazorDatasheet.Portal;

public class PortalService
{
    private Dictionary<string, List<RenderFragment>> _portalTargets = new();
    private Dictionary<string, PortalTarget> _portalTargetComponents = new();

    public void RegisterPortalTarget(string id ,PortalTarget target)
    {
        _portalTargets.TryAdd(id, new List<RenderFragment>());
        _portalTargetComponents.TryAdd(id, target);
    }

    public void AddToPortal(string portalTargetId, RenderFragment fragment)
    {
        if (_portalTargets.TryGetValue(portalTargetId, out var fragments))
        {
            fragments.Add(fragment);
            
            if(_portalTargetComponents.TryGetValue(portalTargetId, out var target))
                target.SetRenderFragments(fragments);
        }
    }

    public IEnumerable<RenderFragment> GetFragments(string portalTargetId)
    {
        return _portalTargets.TryGetValue(portalTargetId, out var fragments)
            ? fragments
            : Enumerable.Empty<RenderFragment>();
    }

    public void RenderPortalTarget(string id)
    {
        if(_portalTargetComponents.TryGetValue(id, out var target))
            target.Render();
    }
}