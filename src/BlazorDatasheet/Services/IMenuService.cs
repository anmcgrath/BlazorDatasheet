using BlazorDatasheet.Menu;
using Microsoft.AspNetCore.Components;

namespace BlazorDatasheet.Services;

public interface IMenuService
{
    internal Task RegisterMenu(string id, IMenu menu, string? parentId = null);

    /// <summary>
    /// Shows a menu with the specified <paramref name="menuId"/>
    /// </summary>
    /// <param name="menuId"></param>
    /// <param name="options"></param>
    /// <param name="context"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>True if menu is opened, false otherwise.</returns>
    Task<bool> ShowMenuAsync<T>(string menuId, MenuShowOptions options, T context = default(T));

    /// <summary>
    /// Closes the menu with the specified <paramref name="menuId"/>
    /// </summary>
    /// <param name="menuId"></param>
    /// <param name="closeParent"></param>
    /// <returns>True if menu is closed, false otherwise</returns>
    Task<bool> CloseMenu(string menuId, bool closeParent = false);

    EventHandler<MenuShownEventArgs>? MenuShown { get; set; }
    EventHandler<BeforeMenuShownEventArgs>? BeforeMenuShown { get; set; }

    /// <summary>
    /// Closes all sub-menus of the menu <paramref name="menuId"/>, except for <paramref name="exceptions"/>
    /// </summary>
    /// <param name="menuId">The menu ID that contains the sub-menus to close</param>
    /// <param name="exceptions">A list of sub-menu IDs that should stay open</param>
    /// <returns></returns>
    Task CloseSubMenus(string menuId, string[]? exceptions = null);

    /// <summary>
    /// Returns whether the menu with id <paramref name="id"/> is open.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    bool IsMenuOpen(string id);

    /// <summary>
    /// Returns whether any menu is open.
    /// </summary>
    /// <returns></returns>
    bool IsMenuOpen();

    /// <summary>
    /// Registers a RenderFragment so that it is shown in the menu with id <paramref name="menuId"/> in section no. <paramref name="sectionNo"/>
    /// </summary>
    /// <param name="menuId"></param>
    /// <param name="sectionNo"></param>
    /// <param name="sectionFragment"></param>
    void RegisterFragment(string menuId, int sectionNo, RenderFragment<object?> sectionFragment);

    /// <summary>
    /// Returns render fragments that have been registered to the menu <paramref name="menuId"/> in section no. <paramref name="sectionNo"/>
    /// </summary>
    /// <param name="menuId"></param>
    /// <param name="sectionNo"></param>
    /// <returns></returns>
    List<RenderFragment<object?>> GetFragments(string menuId, int sectionNo);
}