namespace BlazorDatasheet.Server.Data;

public class Person
{
    public int Id { get; set; }
    private string _firstName;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public double? Age { get; set; }
    public bool Checked { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Now;
}