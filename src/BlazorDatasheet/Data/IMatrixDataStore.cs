namespace BlazorDatasheet.Data;

public interface IMatrixDataStore<T>
{
    public bool Contains(int row, int col);
    public T? Get(int row, int col);
    public void Set(int row, int col, T value);
    public void InsertRowAt(int row);
    /// <summary>
    /// Finds the next non-empty row number in the column. Returns -1 if no non-empty rows exist after the row
    /// </summary>
    /// <param name="col"></param>
    /// <param name="row"></param>
    /// <returns></returns>
    public int GetNextNonBlankRow(int col, int row);
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