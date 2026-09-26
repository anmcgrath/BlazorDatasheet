using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Data;

/// <summary>
/// Sheet data that is keyed by position and so must move when rows or columns are inserted or removed.
/// Every store registered on a sheet is shifted by the insert/remove commands and restored when they are undone,
/// in registration order.
/// </summary>
internal interface IRowColShiftingStore
{
    /// <summary>Shifts the store for an insert and returns the data needed to undo it.</summary>
    object? InsertRowColAt(int index, int count, Axis axis);

    /// <summary>Shifts the store for a removal and returns the data needed to undo it.</summary>
    object? RemoveRowColAt(int index, int count, Axis axis);

    /// <summary>Undoes an insert or removal, given the data it returned.</summary>
    void Restore(object? restoreData);
}

/// <summary>
/// Adapts an existing store with its own typed restore data to <see cref="IRowColShiftingStore"/>.
/// </summary>
internal sealed class ShiftingStoreAdapter<TRestore> : IRowColShiftingStore where TRestore : class
{
    private readonly Func<int, int, Axis, TRestore?> _insert;
    private readonly Func<int, int, Axis, TRestore?> _remove;
    private readonly Action<TRestore> _restore;

    public ShiftingStoreAdapter(
        Func<int, int, Axis, TRestore?> insert,
        Func<int, int, Axis, TRestore?> remove,
        Action<TRestore> restore)
    {
        _insert = insert;
        _remove = remove;
        _restore = restore;
    }

    public object? InsertRowColAt(int index, int count, Axis axis) => _insert(index, count, axis);

    public object? RemoveRowColAt(int index, int count, Axis axis) => _remove(index, count, axis);

    public void Restore(object? restoreData)
    {
        if (restoreData is TRestore data)
            _restore(data);
    }
}
