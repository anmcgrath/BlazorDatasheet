namespace BlazorDatasheet.Formula.Core.Interpreter.Lexing;

public enum LexerReferenceState
{
    None,
    ValidCellRange,
    ValidRowRange,
    ValidColRange
}