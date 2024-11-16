namespace BlazorDatasheet.Virtualise;

public struct VirtualRowArgs
{
    public int Row { get; }
    public int ColStart { get; }

    public int ColEnd { get; }
    public List<int> VisibleColIndices { get; }
    public int RowOffset { get; }

    public VirtualRowArgs(int row, int rowOffset, int colStart, int colEnd, List<int> visibleColIndices)
    {
        Row = row;
        ColStart = colStart;
        RowOffset = rowOffset;
        ColEnd = colEnd;
        VisibleColIndices = visibleColIndices;
    }
}