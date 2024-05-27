using Microsoft.AspNetCore.Components;

namespace BlazorDatasheet;

public class SheetComponentBase : ComponentBase, IHandleEvent
{
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);
}