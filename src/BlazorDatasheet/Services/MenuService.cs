using BlazorDatasheet.Menu;
using Microsoft.JSInterop;

namespace BlazorDatasheet.Services;

public class MenuService : IMenuService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _menuJs;
    private bool _isInitialized;

    public EventHandler<MenuShownEventArgs>? MenuShown { get; set; }
    public EventHandler<BeforeMenuShownEventArgs>? BeforeMenuShown { get; set; }

    public async Task CloseSubMenus(string menuId)
    {
        await _menuJs!.InvokeVoidAsync("closeSubMenus", menuId);
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

        _menuJs = await module.InvokeAsync<IJSObjectReference>(
            "getMenuService");

        _isInitialized = true;
    }

    public async Task RegisterMenu(string id, string? parentId = null)
    {
        await Init();
        await _menuJs!.InvokeVoidAsync("registerMenu", id, parentId);
    }

    public async Task<bool> ShowMenu<T>(string menuId, MenuShowOptions options, T context = default(T))
    {
        await Init();
        var beforeArgs = new BeforeMenuShownEventArgs(menuId, context);
        BeforeMenuShown?.Invoke(this, beforeArgs);
        if (beforeArgs.Cancel)
            return false;
        MenuShown?.Invoke(this, new MenuShownEventArgs(menuId, context));
        await _menuJs!.InvokeVoidAsync("showMenu", menuId, options);
        return true;
    }

    public async Task<bool> CloseMenu(string menuId, bool closeParent = false)
    {
        await Init();
        await _menuJs!.InvokeVoidAsync("closeMenu", menuId, closeParent);
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_menuJs != null) await _menuJs.DisposeAsync();
    }
}