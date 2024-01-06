namespace BlazorDatasheet.Formula.Core.Interpreter2.Parsing;

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