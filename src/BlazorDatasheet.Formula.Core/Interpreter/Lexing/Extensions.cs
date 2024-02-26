namespace BlazorDatasheet.Formula.Core.Interpreter.Lexing;

public static class Extensions
{
    internal static int GetBinaryOperatorPrecedence(this Tag tag)
    {
        switch (tag)
        {
            case Tag.ColonToken:
                return 8;
            case Tag.AmpersandToken:
                return 7;
            case Tag.StarToken:
            case Tag.SlashToken:
                return 6;
            case Tag.PlusToken:
            case Tag.MinusToken:
                return 5;
            case Tag.EqualsToken:
            case Tag.NotEqualToToken:
                return 4;
            case Tag.LessThanToken:
            case Tag.LessThanOrEqualToToken:
            case Tag.GreaterThanToken:
            case Tag.GreaterThanOrEqualToToken:
                return 3;
            default:
                return 0;
        }
    }
}