using BlazorDatasheet.Edit;
using BlazorDatasheet.Interfaces;

namespace BlazorDatasheet.Events.Edit;

public class EditBeginEventArgs
{
    public IReadOnlyCell Cell { get; }
    public string? EditValue { get; }
    public string Type { get; }
    public bool IsSoftEdit { get; }
    public EditEntryMode Mode { get; }
    public string? Key { get; }

    public EditBeginEventArgs(IReadOnlyCell cell,
        string? editValue,
        string type,
        bool isSoftEdit,
        EditEntryMode mode,
        string? key)
    {
        Cell = cell;
        EditValue = editValue;
        Type = type;
        IsSoftEdit = isSoftEdit;
        Mode = mode;
        Key = key;
    }
}