using System.Linq;
using BlazorDatasheet.DataStructures.Graph;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Geometry;

public class GraphTests
{
    [Test]
    public void Topological_Sort_Sorts_Correct_Order()
    {
        var dg = new DependencyGraph();
        var v1 = new TestVertex("1");
        var v2 = new TestVertex("2");
        var v3 = new TestVertex("3");
        var v4 = new TestVertex("4");
        dg.AddEdge(v1, v3);
        dg.AddEdge(v3, v2);
        dg.AddEdge(v4, v1);

        Assert.AreEqual(dg.V, 4);
        Assert.AreEqual(dg.E, 3);

        var order = dg
            .TopologicalSort()
            .Select(x => x.Key);

        var orderStr = string.Join("", order);
        Assert.AreEqual("4132", orderStr);
    }

    [Test]
    public void Add_Remove_Vertices_Test()
    {
        var dg = new DependencyGraph();
        var v1 = new TestVertex("1");
        var v2 = new TestVertex("2");
        var v3 = new TestVertex("3");
        dg.AddEdge(v1, v2);
        dg.AddEdge(v2, v3);
        Assert.AreEqual(3, dg.V);
        Assert.AreEqual(2, dg.E);
        Assert.AreEqual(1, dg.Prec(v2).Count());

        dg.RemoveVertex(v1);
        Assert.AreEqual(1, dg.V);
        Assert.AreEqual(1, dg.E);


        Assert.AreEqual(0, dg.Prec(v2).Count());

        // Add edges & vertices back in

        dg.AddEdge(v1, v2);
        dg.AddEdge(v2, v3);
        Assert.AreEqual(2, dg.V);
        Assert.AreEqual(2, dg.E);
        Assert.AreEqual(1, dg.Prec(v2).Count());
    }
}

public class TestVertex : Vertex
{
    public override string Key { get; }

    public TestVertex(string key)
    {
        Key = key;
    }
}