using BlazorDatasheet.Model;

namespace BlazorDatasheet.Edit;

public class EndEditEventArgs
{
    public string EditedValue { get; }
    public int DRow { get; set; }
    public int DCol { get; set; }

    public EndEditEventArgs(string editedValue, int dRow, int dCol)
    {
        EditedValue = editedValue;
        DRow = dRow;
        DCol = dCol;
    }
}