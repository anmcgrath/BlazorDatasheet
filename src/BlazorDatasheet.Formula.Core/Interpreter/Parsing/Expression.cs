namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

public abstract class Node
{
    public abstract NodeKind Kind { get; }
    public abstract IEnumerable<Node> GetChildren();

    public Node? FindNode(Predicate<Node> predicate)
    {
        if (predicate(this))
            return this;

        foreach (var node in GetChildren())
        {
            var findNode = node.FindNode(predicate);
            if (findNode != null)
                return findNode;
        }

        return null;
    }

    public IEnumerable<Node> FindNodes(Predicate<Node> predicate)
    {
        if (predicate(this))
            yield return this;
        foreach (var node in GetChildren())
        {
            foreach (var find in node.FindNodes(predicate))
                yield return find;
        }
    }
}

public abstract class Expression : Node
{
    public abstract string ToExpressionText();
}