namespace BlazorDatasheet.Data;

public interface IMatrixDataStore<T>
{
    public T? Get(int row, int col);
    public void Set(int row, int col, T value);
    public void InsertRowAt(int row);
    /// <summary>
    /// Finds the next non-empty row number in the column. Returns -1 if no non-empty rows exist after the row
    /// </summary>
    /// <param name="col"></param>
    /// <param name="row"></param>
    /// <returns></returns>
    public int GetNextNonEmptyRow(int col, int row);
    void RemoveRowAt(int row);
    
}