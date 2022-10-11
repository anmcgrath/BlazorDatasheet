namespace BlazorDatasheet.Model.Events;

public class SelectionChangedEventArgs
{
    public IEnumerable<Selection> Selections { get; }
    public IEnumerable<Cell> Cells { get; set; }
}