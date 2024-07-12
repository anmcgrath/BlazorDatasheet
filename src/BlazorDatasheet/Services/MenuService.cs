using BlazorDatasheet.Menu;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorDatasheet.Services;

public class MenuService : IMenuService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _menuJs;
    private DotNetObjectReference<MenuService>? _dotNetObjectReference;
    private bool _isInitialized;
    private Dictionary<string, IMenu> _menus = new();

    public EventHandler<MenuShownEventArgs>? MenuShown { get; set; }
    public EventHandler<BeforeMenuShownEventArgs>? BeforeMenuShown { get; set; }

    private readonly Dictionary<(string menuId, int sectionId), List<RenderFragment<object?>>> _customFragments = new();

    private List<string> _openMenus = new();

    public async Task CloseSubMenus(string menuId, string[]? exceptions)
    {
        await _menuJs!.InvokeVoidAsync("closeSubMenus", menuId, exceptions ?? []);
    }

    private string Id = Guid.NewGuid().ToString();

    public MenuService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    private async Task Init()
    {
        if (_isInitialized)
            return;

        var module =
            await _jsRuntime.InvokeAsync<IJSObjectReference>("import",
                "./_content/BlazorDatasheet/js/menu.js");

        _dotNetObjectReference = DotNetObjectReference.Create(this);

        _menuJs = await module.InvokeAsync<IJSObjectReference>(
            "getMenuService", _dotNetObjectReference);

        _isInitialized = true;
    }

    public async Task RegisterMenu(string id, IMenu menu, string? parentId = null)
    {
        await Init();
        await _menuJs!.InvokeVoidAsync("registerMenu", id, parentId);
        if (!_menus.TryAdd(id, menu))
        {
            _menus[id] = menu;
        }
    }

    public async Task<bool> ShowMenuAsync<T>(string menuId, MenuShowOptions options, T context = default(T))
    {
        // If already open, don't re-open
        if (_openMenus.Contains(menuId))
            return false;
        
        await Init();
        var beforeArgs = new BeforeMenuShownEventArgs(menuId, context);
        BeforeMenuShown?.Invoke(this, beforeArgs);
        if (beforeArgs.Cancel)
            return false;

        MenuShown?.Invoke(this, new MenuShownEventArgs(menuId, context));
        _openMenus.Add(menuId);

        await _menuJs!.InvokeVoidAsync("showMenu", menuId, options);
        if (_menus.TryGetValue(menuId, out var menu))
            await menu.OnMenuOpen.InvokeAsync(new MenuShownEventArgs(menuId, context));
        return true;
    }

    public async Task<bool> CloseMenu(string menuId, bool closeParent = false)
    {
        await Init();
        _openMenus.Remove(menuId);

        await _menuJs!.InvokeVoidAsync("closeMenu", menuId, closeParent);
        return true;
    }

    public bool IsMenuOpen(string id)
    {
        return _openMenus.Contains(id);
    }

    public bool IsMenuOpen()
    {
        return _openMenus.Any();
    }

    [JSInvokable(nameof(OnMenuClose))]
    public async Task OnMenuClose(string menuId)
    {
        _openMenus.Remove(menuId);
        if (_menus.TryGetValue(menuId, out var menu))
            await menu.OnMenuClose.InvokeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_menuJs != null) await _menuJs.DisposeAsync();
        try
        {
            _dotNetObjectReference?.Dispose();
        }
        catch (Exception e)
        {
            // ignore
        }
    }

    public void RegisterFragment(string menuId, int sectionNo, RenderFragment<object?> sectionFragment)
    {
        _customFragments.TryAdd((menuId, sectionNo), new List<RenderFragment<object?>>());
        _customFragments[(menuId, sectionNo)].Add(sectionFragment);
    }

    public List<RenderFragment<object?>> GetFragments(string menuId, int sectionNo)
    {
        if (_customFragments.TryGetValue((menuId, sectionNo), out var fragments))
        {
            return fragments;
        }

        return new List<RenderFragment<object?>>();
    }
}