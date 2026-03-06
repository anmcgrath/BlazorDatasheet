namespace BlazorDatasheet.Render.AutoScroll;

public class AutoScrollState
{
    private bool _isSheetSelectionActive;
    private bool _isAutofillDragging;
    private bool _isEditorSelectionActive;

    public bool IsSheetSelectionActive => _isSheetSelectionActive;
    public bool IsAutofillDragging => _isAutofillDragging;
    public bool IsEditorSelectionActive => _isEditorSelectionActive;
    public bool IsActive => _isSheetSelectionActive || _isAutofillDragging || _isEditorSelectionActive;

    public event Action? Changed;

    public void SetSheetSelectionActive(bool isActive) => SetValue(ref _isSheetSelectionActive, isActive);

    public void SetAutofillDragging(bool isDragging) => SetValue(ref _isAutofillDragging, isDragging);

    public void SetEditorSelectionActive(bool isActive) => SetValue(ref _isEditorSelectionActive, isActive);

    private void SetValue(ref bool field, bool value)
    {
        if (field == value)
            return;

        field = value;
        Changed?.Invoke();
    }
}
