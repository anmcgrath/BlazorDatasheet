using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

public class SyntaxTree
{
    public Expression Root { get; }
    public IReadOnlyCollection<string> Errors { get; }
    private readonly List<Reference> _references;
    public IReadOnlyCollection<Reference> References => _references;

    internal SyntaxTree(Expression root, List<Reference> references, List<string> errors)
    {
        _references = references;
        Root = root;
        Errors = errors;
    }
}