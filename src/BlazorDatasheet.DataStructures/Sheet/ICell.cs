namespace BlazorDatasheet.DataStructures.Sheet;

public interface ICell
{
    int Row { get; }
    int Col { get; }
    object GetValue();
}