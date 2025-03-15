namespace BlazorDatasheet.DataStructures.Store;

public interface IRowSource
{
    public int GetNextNonEmptyIndexInRow(int row, int col);
}