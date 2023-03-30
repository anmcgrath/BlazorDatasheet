namespace BlazorDatasheet.DataStructures.Store;

public interface IMatrixDataStore<T>
{
    /// <summary>
    /// Returns whether the the store contains any data at the row, column specified.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool Contains(int row, int col);
    /// <summary>
    /// Returns the data at the row, column specified. If it is empty, returns the default of T.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public T? Get(int row, int col);
    /// <summary>
    /// Sets the data at the row, column specified.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="value"></param>
    public void Set(int row, int col, T value);
    /// <summary>
    /// Inserts a row into the store
    /// </summary>
    /// <param name="row">The index of the row that the new row is inserted AFTER</param>
    public void InsertRowAfter(int row);
    /// <summary>
    /// Inserts a column into the store
    /// </summary>
    /// <param name="col">The index of the column that the new column is inserted AFTER</param>
    public void InsertColAfter(int col);
    /// <summary>
    /// Removes the column specified from the store.
    /// </summary>
    /// <param name="col">The index of the column to remove.</param>
    public void RemoveColAt(int col);

    /// <summary>
    /// Finds the next non-empty row number in the column. Returns -1 if no non-empty rows exist after the row
    /// </summary>
    /// <param name="col"></param>
    /// <param name="row"></param>
    /// <returns>The next non-empty row number in the column. Equals -1 if no non-empty rows exist after the row.</returns>
    public int GetNextNonBlankRow(int col, int row);

    /// <summary>
    /// Removes the row specified from the store.
    /// </summary>
    /// <param name="row"></param>
    void RemoveRowAt(int row);

    /// <summary>
    /// Get non empty cells that exist in the bounds given
    /// </summary>
    /// <param name="r0">The lower row bound</param>
    /// <param name="r1">The upper row bound</param>
    /// <param name="c0">The lower col bound</param>
    /// <param name="c1">The upper col bound</param>
    /// <returns></returns>
    IEnumerable<(int row, int col)> GetNonEmptyPositions(int r0, int r1, int c0, int c1);
}