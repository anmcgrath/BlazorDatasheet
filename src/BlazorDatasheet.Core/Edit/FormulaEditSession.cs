using System.Text;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events.Selection;
using BlazorDatasheet.Core.Selecting;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Lexing;

namespace BlazorDatasheet.Core.Edit;

/// <summary>
/// The state of the formula being edited: the references in the edit text and the picking of references
/// from the sheet. Editors, whether inside the sheet or outside it, are views over this state and
/// <see cref="Editor.EditValue"/>, so any number of them stay in step.
/// </summary>
public class FormulaEditSession
{
    private readonly Editor _editor;

    /// <summary>
    /// The sheet that is being edited.
    /// </summary>
    public Sheet Sheet => _editor.Sheet;

    private IReadOnlyList<FormulaReferenceSpan> _references = Array.Empty<FormulaReferenceSpan>();
    private SelectionInputManager? _pickInput;

    /// <summary>
    /// The start of the text owned by the current pick, or -1 if there is no pick.
    /// </summary>
    private int _pickStart = -1;

    private int _pickLength;

    /// <summary>
    /// The latched text selection. -1 means that it isn't known, in which case it is the end of the text.
    /// </summary>
    private int _selectionStart = -1;

    private int _selectionEnd = -1;
    private bool _selectionReported;
    private string _lastText = string.Empty;
    private bool _isApplyingText;

    internal FormulaEditSession(Editor editor)
    {
        _editor = editor;
        _editor.EditBegin += (_, _) => Reset(true);
        _editor.EditFinished += (_, _) => Reset(false);
        _editor.EditValueChanged += (_, _) => OnEditValueChanged();
    }

    /// <summary>
    /// Whether the edit value is a formula.
    /// </summary>
    public bool IsFormula => _editor.IsEditing && FormulaEngine.FormulaEngine.IsFormula(_editor.EditValue);

    /// <summary>
    /// The references in the edit value, in text order. Named references are resolved to their regions.
    /// A reference may be to another sheet - see <see cref="FormulaReferenceSpan.SheetName"/>.
    /// </summary>
    public IReadOnlyList<FormulaReferenceSpan> References => _references;

    /// <summary>
    /// Fired when <see cref="References"/> changes.
    /// </summary>
    public event EventHandler? ReferencesChanged;

    /// <summary>
    /// Whether references can be picked from the sheet. Set by the editor that is showing the edit, because only
    /// it knows whether it shows formula text. Reset when an edit begins or finishes.
    /// </summary>
    public bool IsPickingEnabled { get; set; }

    /// <summary>
    /// The start of the text selection in the editor that owns input.
    /// </summary>
    public int SelectionStart => _selectionStart < 0 ? _editor.EditValue.Length : _selectionStart;

    /// <summary>
    /// The end of the text selection in the editor that owns input. Equal to <see cref="SelectionStart"/> for a caret.
    /// </summary>
    public int SelectionEnd => _selectionEnd < 0 ? _editor.EditValue.Length : _selectionEnd;

    /// <summary>
    /// Whether a picked reference currently owns part of the edit text. Further picking replaces that text.
    /// </summary>
    public bool IsPicking => _pickStart >= 0;

    /// <summary>
    /// Whether a reference is being dragged out with the pointer.
    /// </summary>
    public bool IsDragging => _pickInput?.Selection.IsSelecting == true;

    /// <summary>
    /// Fired when the region being picked changes, so that it can be scrolled into view.
    /// </summary>
    public event EventHandler<ActiveRegionChangedEvent>? PickRegionChanged;

    /// <summary>
    /// Fired when <see cref="IsDragging"/> changes.
    /// </summary>
    public event EventHandler<bool>? DraggingChanged;

    /// <summary>
    /// The caret position that the editor owning input should apply, after the session has changed the edit text.
    /// Null once the text has been changed by anything else.
    /// </summary>
    public int? PendingCaret { get; private set; }

    /// <summary>
    /// The editor that the user is typing into. Focus returns to it after picking with the pointer.
    /// </summary>
    public object? InputOwner { get; private set; }

    public void SetInputOwner(object? owner) => InputOwner = owner;

    /// <summary>
    /// Fired when the <see cref="InputOwner"/> should take focus back from the sheet.
    /// </summary>
    public event EventHandler? FocusRequested;

    /// <summary>
    /// Records the text selection of the editor that owns input. Negative values, which editors report when
    /// they lose the selection, are ignored so that the last known selection survives the sheet taking focus.
    /// </summary>
    public void SetTextSelection(int start, int end)
    {
        if (start < 0 || end < 0)
            return;

        // The pointer is on the sheet, so this isn't the user moving the caret. It is the editor's text
        // changing under a selection that it still holds.
        if (IsDragging)
            return;

        _selectionReported = true;
        _selectionStart = Math.Min(start, end);
        _selectionEnd = Math.Max(start, end);

        // moving the caret away from a picked reference leaves that reference alone from now on.
        if (IsPicking && (_selectionStart != _selectionEnd || _selectionStart != _pickStart + _pickLength))
            EndPick();
    }

    /// <summary>
    /// Whether a new reference can be inserted at the caret, which is the case after an operator,
    /// a separator or an opening bracket.
    /// </summary>
    public bool CanAcceptReference
    {
        get
        {
            if (!IsFormula)
                return false;

            var text = _editor.EditValue;
            var start = Math.Clamp(SelectionStart, 0, text.Length);
            var tokens = new Lexer().Lex(text.AsSpan(0, start), Sheet.FormulaEngine.Options);
            if (tokens.Count <= 1)
                return false;

            var tag = tokens[^2].Tag; // EoF is last
            return tag.GetBinaryOperatorPrecedence() > 0 ||
                   tag == Tag.CommaToken ||
                   tag == Tag.SemiColonToken ||
                   tag == Tag.EqualsToken ||
                   tag == Tag.ColonToken ||
                   tag == Tag.LeftParenthToken;
        }
    }

    /// <summary>
    /// The reference that the caret is inside or touching, if any.
    /// </summary>
    private FormulaReferenceSpan? GetReferenceAtCaret()
    {
        if (SelectionStart != SelectionEnd)
            return null;

        var caret = SelectionStart;
        return _references.FirstOrDefault(x =>
            x.Kind != FormulaReferenceSpanKind.Named && x.TextStart <= caret && caret <= x.TextEnd);
    }

    /// <summary>
    /// Whether references can currently be picked from a sheet, given somewhere in the text to put them.
    /// </summary>
    public bool CanPick => IsPickingEnabled && IsFormula;

    /// <summary>
    /// The sheet that references are being picked from, which is any sheet in the workbook.
    /// </summary>
    public Sheet PickSheet => _pickSheet ?? Sheet;

    private Sheet? _pickSheet;

    /// <summary>
    /// Handles a pointer down on the cell at <paramref name="row"/>, <paramref name="col"/>.
    /// Returns whether it was used to pick a reference.
    /// </summary>
    public bool HandlePointerDown(int row, int col, bool shift, bool ctrl, bool meta) =>
        HandlePointerDown(Sheet, row, col, shift, ctrl, meta);

    /// <summary>
    /// Handles a pointer down on a cell of <paramref name="sheet"/>, which is either the sheet being edited or
    /// another in its workbook. Returns whether it was used to pick a reference.
    /// </summary>
    public bool HandlePointerDown(Sheet sheet, int row, int col, bool shift, bool ctrl, bool meta)
    {
        if (!CanPick || !ReferenceEquals(sheet.Workbook, Sheet.Workbook))
            return false;

        if (!TryBeginPick(allowReplaceAtCaret: true))
            return false;

        // a reference is to one sheet, so the regions picked from another are left behind
        if (!ReferenceEquals(sheet, PickSheet))
            SetPickSheet(sheet);

        _pickInput!.HandlePointerDown(row, col, shift, ctrl, meta, 1);
        ApplyPick();
        return true;
    }

    /// <summary>
    /// Handles the pointer moving over the cell at <paramref name="row"/>, <paramref name="col"/>.
    /// Returns whether it was used to pick a reference.
    /// </summary>
    public bool HandlePointerOver(int row, int col) => HandlePointerOver(Sheet, row, col);

    /// <summary>
    /// Handles the pointer moving over a cell of <paramref name="sheet"/>. Returns whether it was used to pick
    /// a reference, which it isn't when the drag began on a different sheet.
    /// </summary>
    public bool HandlePointerOver(Sheet sheet, int row, int col)
    {
        if (!IsPicking || !IsDragging || !ReferenceEquals(sheet, PickSheet))
            return false;

        _pickInput!.HandlePointerOver(row, col);
        ApplyPick();
        return true;
    }

    /// <summary>
    /// Handles the pointer being released. Returns whether a reference was being dragged out.
    /// </summary>
    public bool HandlePointerUp()
    {
        if (!IsPicking || !IsDragging)
            return false;

        _pickInput!.HandleWindowMouseUp();
        ApplyPick();
        FocusRequested?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>
    /// Handles an arrow key. Picks with the keyboard only during a soft edit, because otherwise the arrow keys
    /// belong to the caret. Returns whether it was used to pick a reference.
    /// </summary>
    public bool HandleArrowKey(Offset offset, bool shift)
    {
        if (!CanPick || !_editor.IsSoftEdit || _editor.EditCell == null)
            return false;

        if (!TryBeginPick(allowReplaceAtCaret: false))
            return false;

        // the arrow keys move from the cell being edited, so they pick from its sheet
        if (!ReferenceEquals(PickSheet, Sheet))
            SetPickSheet(Sheet);

        if (_pickInput!.Selection.IsEmpty())
            _pickInput.Selection.Set(_editor.EditCell.Row, _editor.EditCell.Col);

        _pickInput.HandleArrowKeyDown(shift, offset);
        ApplyPick();
        return true;
    }

    /// <summary>
    /// Makes sure that there is a pick that owns part of the text. A new pick either inserts at the caret or,
    /// if <paramref name="allowReplaceAtCaret"/>, takes over the reference at the caret.
    /// </summary>
    private bool TryBeginPick(bool allowReplaceAtCaret)
    {
        if (IsPicking)
            return true;

        var length = _editor.EditValue.Length;
        var start = Math.Clamp(SelectionStart, 0, length);
        var end = Math.Clamp(SelectionEnd, 0, length);

        // A caret that touches a reference never inserts, because that would run two references together.
        var reference = GetReferenceAtCaret();
        if (reference != null && allowReplaceAtCaret)
        {
            _pickStart = reference.TextStart;
            _pickLength = reference.TextLength;
        }
        else if (reference == null && CanAcceptReference)
        {
            _pickStart = start;
            _pickLength = end - start;
        }
        else
            return false;

        _pickInput!.Clear();
        return true;
    }

    private void EndPick()
    {
        if (!IsPicking)
            return;

        _pickStart = -1;
        _pickLength = 0;
        _pickInput?.Clear();
    }

    /// <summary>
    /// Writes the picked regions over the text that the pick owns.
    /// </summary>
    private void ApplyPick()
    {
        var selection = _pickInput!.Selection;
        var separator = Sheet.FormulaEngine.Options.SeparatorSettings.FuncParameterSeparator;
        var prefix = ReferenceEquals(PickSheet, Sheet) ? string.Empty : RangeText.SheetPrefix(PickSheet.Name);

        var sb = new StringBuilder();
        foreach (var region in selection.Regions)
        {
            if (sb.Length > 0)
                sb.Append(separator);
            sb.Append(prefix).Append(RangeText.RegionToText(region));
        }

        if (selection.SelectingRegion != null)
        {
            if (sb.Length > 0)
                sb.Append(separator);
            sb.Append(prefix).Append(RangeText.RegionToText(selection.SelectingRegion));
        }

        var pickStart = _pickStart;
        var text = sb.ToString();
        ApplyText(pickStart, _pickLength, text);
        _pickStart = pickStart;
        _pickLength = text.Length;
    }

    /// <summary>
    /// Moves or resizes the reference at <paramref name="referenceIndex"/> in <see cref="References"/> so that
    /// it refers to <paramref name="region"/>, keeping its sheet name and which parts of it are fixed.
    /// </summary>
    public bool ReplaceReference(int referenceIndex, IRegion region)
    {
        if (referenceIndex < 0 || referenceIndex >= _references.Count)
            return false;

        var reference = _references[referenceIndex];
        if (reference.Kind == FormulaReferenceSpanKind.Named)
            return false;

        var text = GetSheetPrefix(reference.SheetName) +
                   RangeText.RegionToText(region, reference.IsStartColFixed, reference.IsEndColFixed,
                       reference.IsStartRowFixed, reference.IsEndRowFixed);

        EndPick();
        ApplyText(reference.TextStart, reference.TextLength, text);
        return true;
    }

    /// <summary>
    /// Replaces part of the edit text, leaving the caret <paramref name="caretOffset"/> characters
    /// after <paramref name="start"/>. By default the caret is left after the new text.
    /// </summary>
    public void ReplaceText(int start, int length, string text, int? caretOffset = null)
    {
        if (!_editor.IsEditing)
            return;

        EndPick();
        ApplyText(start, length, text, caretOffset);
    }

    private void ApplyText(int start, int length, string text, int? caretOffset = null)
    {
        var current = _editor.EditValue;
        start = Math.Clamp(start, 0, current.Length);
        length = Math.Clamp(length, 0, current.Length - start);

        var caret = start + Math.Clamp(caretOffset ?? text.Length, 0, text.Length);
        _selectionStart = caret;
        _selectionEnd = caret;
        PendingCaret = caret;

        _isApplyingText = true;
        try
        {
            _editor.EditValue = string.Concat(current.AsSpan(0, start), text, current.AsSpan(start + length));
        }
        finally
        {
            _isApplyingText = false;
        }
    }

    private static string GetSheetPrefix(string? sheetName)
    {
        if (sheetName == null)
            return string.Empty;

        return RangeText.SheetPrefix(sheetName);
    }

    private void OnEditValueChanged()
    {
        var text = _editor.EditValue;

        if (!_isApplyingText)
        {
            // the text was changed by typing, so the pick no longer owns any of it.
            _pickStart = -1;
            _pickLength = 0;
            if (_pickInput?.Selection.IsEmpty() == false || IsDragging)
                _pickInput!.Clear();

            // keep a caret that was at the end of the text there until the editor reports otherwise
            if (!_selectionReported || _selectionStart == _lastText.Length)
            {
                _selectionStart = -1;
                _selectionEnd = -1;
            }

            PendingCaret = null;
        }

        _lastText = text;
        ScanReferences();
    }

    private void ScanReferences()
    {
        var hadReferences = _references.Count > 0;

        if (!IsFormula)
            _references = Array.Empty<FormulaReferenceSpan>();
        else
        {
            var engine = Sheet.FormulaEngine;
            _references = FormulaReferenceScanner
                .Scan(_editor.EditValue, engine.Options, engine.VariableExists)
                .Select(ResolveNamedReference)
                .ToList();
        }

        if (hadReferences || _references.Count > 0)
        {
            ReferencesChanged?.Invoke(this, EventArgs.Empty);
            // the references may be to other sheets, which the views of those sheets show
            Sheet.Workbook.NotifyFormulaEditReferencesChanged();
        }
    }

    private FormulaReferenceSpan ResolveNamedReference(FormulaReferenceSpan span)
    {
        if (span.Kind != FormulaReferenceSpanKind.Named || span.Name == null)
            return span;

        var reference = Sheet.FormulaEngine.GetVariableReferences(span.Name)
            .FirstOrDefault(x => x.Region is not EmptyRegion);

        return reference == null
            ? span
            : span with { Region = reference.Region, SheetName = reference.SheetName };
    }

    /// <summary>
    /// Starts picking from <paramref name="sheet"/> with nothing picked, or stops picking if it is null.
    /// </summary>
    private void SetPickSheet(Sheet? sheet)
    {
        if (_pickInput != null)
        {
            _pickInput.Selection.ActiveRegionChanged -= OnPickActiveRegionChanged;
            _pickInput.Selection.SelectingChanged -= OnPickSelectingChanged;
        }

        if (_wasDragging)
        {
            _wasDragging = false;
            DraggingChanged?.Invoke(this, false);
        }

        _pickInput = null;
        _pickSheet = sheet;
        if (sheet != null)
        {
            // Picking uses its own selection, which isn't subject to the sheet's selection rules.
            _pickInput = new SelectionInputManager(new Selection(sheet));
            _pickInput.Selection.ActiveRegionChanged += OnPickActiveRegionChanged;
            _pickInput.Selection.SelectingChanged += OnPickSelectingChanged;
        }
    }

    private void Reset(bool isEditing)
    {
        SetPickSheet(isEditing ? Sheet : null);

        _pickStart = -1;
        _pickLength = 0;
        _selectionStart = -1;
        _selectionEnd = -1;
        _selectionReported = false;
        _lastText = _editor.EditValue;
        PendingCaret = null;
        IsPickingEnabled = false;
        InputOwner = null;
        ScanReferences();
    }

    private bool _wasDragging;

    private void OnPickSelectingChanged(object? sender, IRegion? region)
    {
        var isDragging = region != null;
        if (isDragging == _wasDragging)
            return;

        _wasDragging = isDragging;
        DraggingChanged?.Invoke(this, isDragging);
    }

    private void OnPickActiveRegionChanged(object? sender, ActiveRegionChangedEvent e) =>
        PickRegionChanged?.Invoke(this, e);
}
