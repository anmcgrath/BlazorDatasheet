namespace BlazorDatasheet.Server.Data;

public class Person
{
    public int? Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Category { get; set; }
    public string? Age { get; set; }
    public bool IsFriendly { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Now;
}