namespace BlazorDatasheet.DataStructures.Graph;

public class NamedVertex : Vertex
{
    private readonly string _name;

    public NamedVertex(string name)
    {
        _name = name;
    }

    public override string Key => _name;
}