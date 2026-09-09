namespace BlazorDatasheet.Core.Events.Selection;

/// <summary>The user interaction proposing a selection change.</summary>
public enum SelectionInputKind
{
    PointerStart,
    Drag,
    ShiftExtension,
    ArrowNavigation,
    TabEnterNavigation,
    HeaderSelection
}
