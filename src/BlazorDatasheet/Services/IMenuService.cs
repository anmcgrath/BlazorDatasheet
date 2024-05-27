using BlazorDatasheet.Menu;

namespace BlazorDatasheet.Services;

public interface IMenuService
{
    internal Task RegisterMenu(string id, string? parentId = null);
    Task ShowMenu<T>(string menuId, MenuShowOptions options, T context = default(T));
    Task CloseMenu(string menuId, bool closeParent = false);
    EventHandler<MenuShownEventArgs>? MenuShown { get; set; }
    Task CloseSubMenus(string menuId);
}