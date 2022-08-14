namespace BlazorDatasheet.Wasm.Data;

public class Person
{
    public int Id { get; set; }
    private string _firstName;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public double? Age { get; set; }
    public DateTime EntryDate { get; set; }
}