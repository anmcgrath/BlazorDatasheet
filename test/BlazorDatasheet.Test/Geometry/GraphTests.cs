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
        Assert.AreEqual(1, dg.GetPrecedentsOf(v2).Count());

        dg.RemoveVertex(v1);
        Assert.AreEqual(2, dg.Count);
        Assert.AreEqual(1, dg.E);


        Assert.AreEqual(0, dg.GetPrecedentsOf(v2).Count());

        // Add edges & vertices back in

        dg.AddEdge(v1, v2);
        dg.AddEdge(v2, v3);
        Assert.AreEqual(3, dg.Count);
        Assert.AreEqual(2, dg.E);
        Assert.AreEqual(1, dg.GetPrecedentsOf(v2).Count());
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

        dg.GetPrecedentsOf(w).Should().BeEquivalentTo([v]);
        dg.GetDependentsOf(v).Should().BeEquivalentTo([w]);

        var u = new TestVertex("3");
        dg.Swap(v, u); // now w depends on u

        dg.GetDependentsOf(w).Should().BeEmpty();
        dg.GetPrecedentsOf(w).Should().BeEquivalentTo([u]);
    }

    [Test]
    public void Remove_Vertex_With_Multiple_Edges_Does_Not_Throw()
    {
        var dg = new DependencyGraph();
        var v1 = new TestVertex("1");
        var v2 = new TestVertex("2");
        var v3 = new TestVertex("3");
        var v4 = new TestVertex("4");
        dg.AddEdge(v1, v2);
        dg.AddEdge(v1, v3);
        dg.AddEdge(v4, v1);

        Action action = () => dg.RemoveVertex(v1);

        action.Should().NotThrow();
        dg.HasVertex(v1.Key).Should().BeFalse();
        dg.E.Should().Be(0);
    }

    [Test]
    public void Refresh_Key_Does_Not_Remove_Neighbouring_Vertices()
    {
        var dg = new DependencyGraph<MutableTestVertex>();
        var source = new MutableTestVertex("A");
        var dependent = new MutableTestVertex("B");
        var precedent = new MutableTestVertex("C");
        dg.AddEdge(source, dependent);
        dg.AddEdge(precedent, source);

        source.SetNextKey("A2");
        dg.RefreshKey(source);

        dg.HasVertex("A2").Should().BeTrue();
        dg.HasVertex("B").Should().BeTrue();
        dg.HasVertex("C").Should().BeTrue();
        dg.GetDependentsOf("A2").Select(x => x.Key).Should().BeEquivalentTo(["B"]);
        dg.GetPrecedentsOf("A2").Select(x => x.Key).Should().BeEquivalentTo(["C"]);
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

public class MutableTestVertex : Vertex, IEquatable<MutableTestVertex>
{
    private string _key;
    private string? _nextKey;
    public override string Key => _key;

    public MutableTestVertex(string key)
    {
        _key = key;
    }

    public void SetNextKey(string nextKey)
    {
        _nextKey = nextKey;
    }

    public override void UpdateKey()
    {
        if (_nextKey != null)
        {
            _key = _nextKey;
            _nextKey = null;
        }
    }

    public bool Equals(MutableTestVertex? other)
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
        return Equals((MutableTestVertex)obj);
    }

    public override int GetHashCode()
    {
        return Key.GetHashCode();
    }
}
