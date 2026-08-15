using BlazorDatasheet.Menu;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using static BlazorDatasheet.Util.JsInteropHelper;

namespace BlazorDatasheet.Services;

public class MenuService : IMenuService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _menuJs;
    private DotNetObjectReference<MenuService>? _dotNetObjectReference;
    private Task? _initTask;
    private bool _isDisposed;
    private Dictionary<string, IMenu> _menus = new();

    public EventHandler<MenuShownEventArgs>? MenuShown { get; set; }
    public EventHandler<BeforeMenuShownEventArgs>? BeforeMenuShown { get; set; }

    private readonly Dictionary<(string menuId, int sectionId), List<RenderFragment<object?>>> _customFragments = new();

    private List<string> _openMenus = new();

    public async Task CloseSubMenus(string menuId, string[]? exceptions)
    {
        if (_menuJs == null)
            return;

        await _menuJs.InvokeVoidAsync("closeSubMenus", menuId, exceptions ?? []);
    }

    private string Id = Guid.NewGuid().ToString();

    public MenuService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    // Cached so that callers arriving while initialisation is in flight wait for it
    // rather than continuing with a null _menuJs.
    private Task Init() => _initTask ??= InitCoreAsync();

    private async Task InitCoreAsync()
    {
        if (_isDisposed)
            return;

        var module =
            await _jsRuntime.InvokeAsync<IJSObjectReference>("import",
                "./_content/BlazorDatasheet/js/menu.js");

        if (_isDisposed)
        {
            await DisposeJsObjectReferenceAsync(module);
            return;
        }

        var dotNetObjectReference = DotNetObjectReference.Create(this);

        try
        {
            var menuJs = await module.InvokeAsync<IJSObjectReference>(
                "getMenuService", dotNetObjectReference);

            if (_isDisposed)
            {
                await DisposeJsObjectReferenceAsync(menuJs);
                return;
            }

            _menuJs = menuJs;
            _dotNetObjectReference = dotNetObjectReference;
            dotNetObjectReference = null;
        }
        finally
        {
            dotNetObjectReference?.Dispose();
            await DisposeJsObjectReferenceAsync(module);
        }
    }

    public async Task RegisterMenu(string id, IMenu menu, string? parentId = null)
    {
        await Init();
        if (_menuJs == null)
            return;

        await _menuJs.InvokeVoidAsync("registerMenu", id, parentId);
        if (!_menus.TryAdd(id, menu))
        {
            _menus[id] = menu;
        }
    }

    public async Task UnregisterMenu(string id)
    {
        try
        {
            if (_menuJs != null)
                await _menuJs.InvokeVoidAsync("unregisterMenu", id);
        }
        catch (Exception)
        {
            // Ignore
        }
    }

    public async Task<bool> ShowMenuAsync<T>(string menuId, MenuTargetOptions options, T context = default(T))
    {
        // If already open, don't re-open
        if (_openMenus.Contains(menuId))
            return false;

        await Init();
        if (_menuJs == null)
            return false;

        var beforeArgs = new BeforeMenuShownEventArgs(menuId, context);
        BeforeMenuShown?.Invoke(this, beforeArgs);
        if (beforeArgs.Cancel)
            return false;

        MenuShown?.Invoke(this, new MenuShownEventArgs(menuId, context));
        _openMenus.Add(menuId);

        await _menuJs.InvokeVoidAsync("showMenu", menuId, options);
        if (_menus.TryGetValue(menuId, out var menu))
            await menu.OnMenuOpen.InvokeAsync(new MenuShownEventArgs(menuId, context));
        return true;
    }

    public async Task<bool> CloseMenu(string menuId, bool closeParent = false)
    {
        await Init();
        _openMenus.Remove(menuId);

        if (_menuJs == null)
            return false;

        await _menuJs.InvokeVoidAsync("closeMenu", menuId, closeParent);
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
        _isDisposed = true;
        _initTask = null;

        var menuJs = _menuJs;
        _menuJs = null;
        var dotNetObjectReference = _dotNetObjectReference;
        _dotNetObjectReference = null;

        try
        {
            if (menuJs != null)
                await DisposeJsObjectReferenceAsync(menuJs);
        }
        catch (Exception)
        {
            // ignore
        }
        finally
        {
            dotNetObjectReference?.Dispose();
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