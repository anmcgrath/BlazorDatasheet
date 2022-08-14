namespace BlazorDatasheet.Model;

public class Cell
{
    public string Type { get; set; } = "text";
    public string? Value { get; set; }
    public string Format { get; set; } = "";
}