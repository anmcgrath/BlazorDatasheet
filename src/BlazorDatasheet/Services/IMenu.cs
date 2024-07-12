using BlazorDatasheet.Menu;
using Microsoft.AspNetCore.Components;

namespace BlazorDatasheet.Services;

public interface IMenu
{
    EventCallback<MenuShownEventArgs> OnMenuOpen { get; set; }
    EventCallback OnMenuClose { get; set; }
}