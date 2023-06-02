using BlazorDatasheet.FormulaEngine.Interpreter.References;

namespace BlazorDatasheet.FormulaEngine.Interpreter.Syntax;

public sealed class SyntaxTree
{
    internal SyntaxTree(IEnumerable<string> diagnostics, IEnumerable<Reference> references, ExpressionSyntax root,
        SyntaxToken endOfFileToken)
    {
        Diagnostics = diagnostics.ToArray();
        Root = root;
        EndOfFileToken = endOfFileToken;
        References = references.ToList();
    }

    public IReadOnlyList<string> Diagnostics { get; }
    public ExpressionSyntax Root { get; }
    public SyntaxToken EndOfFileToken { get; }

    public List<Reference> References { get; }

    public static SyntaxTree Parse(string text)
    {
        var parser = new Parser();
        return parser.Parse(new Lexer(), text);
    }
}