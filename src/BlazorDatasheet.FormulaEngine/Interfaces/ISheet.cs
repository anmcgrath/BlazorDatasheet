namespace BlazorDatasheet.FormulaEngine.Interfaces;

public interface ISheet
{
    public ICell GetCell(int row, int col);
    public IRange GetRange(int rowStart, int rowStop, int colStart, int colStop);
    public IRange GetColumn(int colStart, int colStop);
    public IRange GetRowRange(int rowStart, int rowStop);
    public void SetValue(int row, int col, object value);
}