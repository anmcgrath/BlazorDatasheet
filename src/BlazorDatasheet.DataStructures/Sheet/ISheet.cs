namespace BlazorDatasheet.DataStructures.Sheet;

public interface ISheet
{
    public object GetCellValue(int row, int col);
    public IRange GetRange(int rowStart, int rowStop, int colStart, int colStop);
    public IRange GetColumnRange(int colStart, int colStop);
    public IRange GetRowRange(int rowStart, int rowStop);
    public bool TrySetCellValue(int row, int col, object value);
    public void Pause();
    public void Resume();
}