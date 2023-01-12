using BlazorDatasheet.FormulaEngine.Graph;

namespace BlazorDatasheet.FormulaEngine.Test;

public class TestVertex : Vertex
{
    public override string Key { get; }

    public TestVertex(string key)
    {
        Key = key;
    }
}