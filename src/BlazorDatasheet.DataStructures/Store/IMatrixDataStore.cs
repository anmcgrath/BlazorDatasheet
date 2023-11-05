using System.Collections;
using BlazorDatasheet.DataStructures.Geometry;

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
    public MatrixRestoreData<T>  Set(int row, int col, T value);

    /// <summary>
    /// Removes the value at the row/column from the store but does not affect the rows/columns around it.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public MatrixRestoreData<T> Clear(int row, int col);

    public MatrixRestoreData<T> Clear(IEnumerable<CellPosition> positions);

    /// <summary>
    /// Clears data inside the given region but does not affect the rows/columns arround it.
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public MatrixRestoreData<T>  Clear(IRegion region);

    /// <summary>
    /// Clears data inside the specified regions but does not affect the rows/columsn around it.
    /// </summary>
    /// <param name="regions"></param>
    /// <returns></returns>
    public MatrixRestoreData<T> Clear(IEnumerable<IRegion> regions);

    /// <summary>
    /// Inserts a row into the store
    /// </summary>
    /// <param name="row">The index of the row that the new row will now be.</param>
    /// <param name="nRows">The number of rows to inser</param>
    public void InsertRowAt(int row, int nRows);

    /// <summary>
    /// Inserts a column into the store
    /// </summary>
    /// <param name="col">The index of the column that the new column is inserted AFTER</param>
    public void InsertColAt(int col, int nCols);

    /// <summary>
    /// Removes the column specified from the store and returns the values that were removed.
    /// </summary>
    /// <param name="col">The index of the column to remove.</param>
    public MatrixRestoreData<T>  RemoveColAt(int col, int nRow);

    /// <summary>
    /// Finds the next non-empty row number in the column. Returns -1 if no non-empty rows exist after the row
    /// </summary>
    /// <param name="col"></param>
    /// <param name="row"></param>
    /// <returns>The next non-empty row number in the column. Equals -1 if no non-empty rows exist after the row.</returns>
    public int GetNextNonBlankRow(int col, int row);

    /// <summary>
    /// Removes the row specified from the store and returns the values that were removed.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="nRows"></param>
    public MatrixRestoreData<T>  RemoveRowAt(int row, int nRows);

    /// <summary>
    /// Get non empty cells that exist in the bounds given
    /// </summary>
    /// <param name="r0">The lower row bound</param>
    /// <param name="r1">The upper row bound</param>
    /// <param name="c0">The lower col bound</param>
    /// <param name="c1">The upper col bound</param>
    /// <returns></returns>
    IEnumerable<CellPosition> GetNonEmptyPositions(int r0, int r1, int c0, int c1);

    /// <summary>
    /// Gets non empty cells that exist in the region given.
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    IEnumerable<CellPosition> GetNonEmptyPositions(IRegion region);

    /// <summary>
    /// Copy the data in <paramref name="fromRegion"/> to the position <paramref name="toPosition"/>
    /// </summary>
    /// <param name="fromRegion"></param>
    /// <param name="toPosition"></param>
    /// <returns></returns>
    MatrixRestoreData<T> Copy(IRegion fromRegion, IRegion toPosition);

    void Restore(MatrixRestoreData<T> restoreData);
}