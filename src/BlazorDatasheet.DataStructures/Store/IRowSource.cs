namespace BlazorDatasheet.DataStructures.Store;

public interface IRowSource
{
    /// <summary>
    /// Finds the next non-empty row number in the column in the direction <paramref name="colDir"/> Returns -1 if no non-empty rows exist after the row
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="colDir"></param>
    /// <returns>The next non-empty row number in the column. Equals -1 if no non-empty rows exist after the row.</returns>
    public int GetNextNonEmptyIndexInRow(int row, int col, int colDir = 1);
}