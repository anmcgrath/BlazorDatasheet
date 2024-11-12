namespace BlazorDatasheet.Virtualise;

public struct VirtualRowArgs
{
    public int Row { get; }
    public int ColStart { get; }
    
    public int ColEnd { get; }
    public int RowOffset { get; }

    public VirtualRowArgs(int row, int rowOffset, int colStart, int colEnd)
    {
        Row = row;
        ColStart = colStart;
        RowOffset = rowOffset;
        ColEnd = colEnd;
    }
}