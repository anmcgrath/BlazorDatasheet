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

    /// <summary>
    /// Shifts the store for blank cells inserted over <paramref name="region"/>, moving the cells after it right
    /// (<see cref="Axis.Col"/>) or down (<see cref="Axis.Row"/>) within the band of rows (or columns) the region covers.
    /// Returns the data needed to undo it.
    /// </summary>
    object? InsertCellsAt(IRegion region, Axis axis);

    /// <summary>
    /// Shifts the store for the cells in <paramref name="region"/> being removed, moving the cells after it left
    /// (<see cref="Axis.Col"/>) or up (<see cref="Axis.Row"/>) within the band of rows (or columns) the region covers.
    /// Returns the data needed to undo it.
    /// </summary>
    object? RemoveCellsAt(IRegion region, Axis axis);

    /// <summary>Undoes an insert or removal (of rows/columns or cells), given the data it returned.</summary>
    void Restore(object? restoreData);
}

/// <summary>
/// Adapts an existing store with its own typed restore data to <see cref="IRowColShiftingStore"/>.
/// </summary>
internal sealed class ShiftingStoreAdapter<TRestore> : IRowColShiftingStore where TRestore : class
{
    private readonly Func<int, int, Axis, TRestore?> _insert;
    private readonly Func<int, int, Axis, TRestore?> _remove;
    private readonly Func<IRegion, Axis, TRestore?> _insertCells;
    private readonly Func<IRegion, Axis, TRestore?> _removeCells;
    private readonly Action<TRestore> _restore;

    public ShiftingStoreAdapter(
        Func<int, int, Axis, TRestore?> insert,
        Func<int, int, Axis, TRestore?> remove,
        Func<IRegion, Axis, TRestore?> insertCells,
        Func<IRegion, Axis, TRestore?> removeCells,
        Action<TRestore> restore)
    {
        _insert = insert;
        _remove = remove;
        _insertCells = insertCells;
        _removeCells = removeCells;
        _restore = restore;
    }

    public object? InsertRowColAt(int index, int count, Axis axis) => _insert(index, count, axis);

    public object? RemoveRowColAt(int index, int count, Axis axis) => _remove(index, count, axis);

    public object? InsertCellsAt(IRegion region, Axis axis) => _insertCells(region, axis);

    public object? RemoveCellsAt(IRegion region, Axis axis) => _removeCells(region, axis);

    public void Restore(object? restoreData)
    {
        if (restoreData is TRestore data)
            _restore(data);
    }
}
