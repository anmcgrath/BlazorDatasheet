using System;
using System.Linq;
using BlazorDatasheet.Core.FormulaEngine;
using BlazorDatasheet.DataStructures.Graph;
using FluentAssertions;
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

        Assert.AreEqual(dg.Count, 4);
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
        Assert.AreEqual(3, dg.Count);
        Assert.AreEqual(2, dg.E);
        Assert.AreEqual(1, dg.Prec(v2).Count());

        dg.RemoveVertex(v1);
        Assert.AreEqual(2, dg.Count);
        Assert.AreEqual(1, dg.E);


        Assert.AreEqual(0, dg.Prec(v2).Count());

        // Add edges & vertices back in

        dg.AddEdge(v1, v2);
        dg.AddEdge(v2, v3);
        Assert.AreEqual(3, dg.Count);
        Assert.AreEqual(2, dg.E);
        Assert.AreEqual(1, dg.Prec(v2).Count());
    }

    [Test]
    public void Swap_Dependent_Vertex_Out_Swaps_Correctly()
    {
        var dg = new DependencyGraph();
        var v = new TestVertex("1");
        var w = new TestVertex("2");
        dg.AddVertex(v);
        dg.AddVertex(w);
        dg.AddEdge(v, w); // w depends on v

        dg.Prec(w).Should().BeEquivalentTo([v]);
        dg.Adj(v).Should().BeEquivalentTo([w]);

        var u = new TestVertex("3");
        dg.Swap(v, u); // now w depends on u

        dg.Adj(w).Should().BeEmpty();
        dg.Prec(w).Should().BeEquivalentTo([u]);
    }
}

public class TestVertex : Vertex, IEquatable<TestVertex>
{
    public override string Key { get; }
    public override void UpdateKey()
    {
    }

    public TestVertex(string key)
    {
        Key = key;
    }

    public bool Equals(TestVertex? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Key == other.Key;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((TestVertex)obj);
    }

    public override int GetHashCode()
    {
        return Key.GetHashCode();
    }
}