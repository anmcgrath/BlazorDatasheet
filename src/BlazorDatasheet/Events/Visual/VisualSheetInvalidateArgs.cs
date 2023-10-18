namespace BlazorDatasheet.Events.Visual;

public class VisualSheetInvalidateArgs
{
    public HashSet<(int row, int col)> DirtyCells { get; }

    public VisualSheetInvalidateArgs(HashSet<(int row, int col)> dirtyCells)
    {
        DirtyCells = dirtyCells;
    }
}