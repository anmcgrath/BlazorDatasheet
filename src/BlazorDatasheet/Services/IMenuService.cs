using BlazorDatasheet.Menu;

namespace BlazorDatasheet.Services;

public interface IMenuService
{
    internal Task RegisterMenu(string id, string? parentId = null);

    /// <summary>
    /// Shows a menu with the specified <paramref name="menuId"/>
    /// </summary>
    /// <param name="menuId"></param>
    /// <param name="options"></param>
    /// <param name="context"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>True if menu is opened, false otherwise.</returns>
    Task<bool> ShowMenu<T>(string menuId, MenuShowOptions options, T context = default(T));

    /// <summary>
    /// Closes the menu with the specified <paramref name="menuId"/>
    /// </summary>
    /// <param name="menuId"></param>
    /// <param name="closeParent"></param>
    /// <returns>True if menu is closed, false otherwise</returns>
    Task<bool> CloseMenu(string menuId, bool closeParent = false);

    EventHandler<MenuShownEventArgs>? MenuShown { get; set; }
    EventHandler<BeforeMenuShownEventArgs>? BeforeMenuShown { get; set; }
    Task CloseSubMenus(string menuId);
}