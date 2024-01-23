namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

public enum NodeKind
{
    BinaryOperation,
    UnaryOperation,
    Literal,
    ParenthesizedExpression,
    FunctionCall,
    ArrayConstant,
    Name,
    Range
}