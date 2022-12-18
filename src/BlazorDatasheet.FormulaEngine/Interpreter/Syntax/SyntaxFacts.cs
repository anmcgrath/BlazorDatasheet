namespace BlazorDatasheet.FormulaEngine.Interpreter.Syntax;

internal static class SyntaxFacts
{
    internal static int GetBinaryOperatorPrecedence(this SyntaxKind kind)
    {
        switch (kind)
        {
            case SyntaxKind.StarToken:
            case SyntaxKind.SlashToken:
                return 6;
            case SyntaxKind.PlusToken:
            case SyntaxKind.MinusToken:
                return 5;

            case SyntaxKind.EqualsToken:
            case SyntaxKind.NotEqualsToken:
                return 4;
            case SyntaxKind.LessThanToken:
            case SyntaxKind.LessThanEqualToToken:
            case SyntaxKind.GreaterThanToken:
            case SyntaxKind.GreaterThanEqualToToken:
                return 3;

            case SyntaxKind.AmpersandAmpersandToken:
            case SyntaxKind.AndKeyword:
                return 2;
            case SyntaxKind.PipePipeToken:
                return 1;
            default:
                return 0;
        }
    }

    internal static int GetUnaryOperatorPrecedence(this SyntaxKind kind)
    {
        switch (kind)
        {
            case SyntaxKind.PlusToken:
            case SyntaxKind.MinusToken:
            case SyntaxKind.BangToken:
                return 7;
            default:
                return 0;
        }
    }

    public static SyntaxKind GetKeywordKind(string text)
    {
        switch (text.ToLower())
        {
            case "true":
                return SyntaxKind.TrueKeyword;
            case "false":
                return SyntaxKind.FalseKeyword;
            case "and":
                return SyntaxKind.AndKeyword;
            default:
                return SyntaxKind.IdentifierToken;
        }
    }
}