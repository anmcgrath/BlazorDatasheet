using BlazorDatasheet.FormulaEngine.Interfaces;
using BlazorDatasheet.FormulaEngine.Interpreter.Functions;
using BlazorDatasheet.FormulaEngine.Interpreter.References;
using BlazorDatasheet.FormulaEngine.Interpreter.Syntax;

namespace BlazorDatasheet.FormulaEngine.Interpreter;

public class FormulaEvaluator
{
    private readonly Environment _environment;
    private readonly ParameterTypeConversion _converter;

    public FormulaEvaluator(Environment environment)
    {
        _environment = environment;
        _converter = new ParameterTypeConversion(environment);
    }

    public object Evaluate(SyntaxTree tree)
    {
        return Evaluate(tree.Root);
    }

    private object Evaluate(SyntaxNode node)
    {
        switch (node.Kind)
        {
            case SyntaxKind.LiteralExpression:
                return EvaluateLiteralExpression((LiteralExpressionSyntax)node);
            case SyntaxKind.BinaryExpression:
                return EvalueBinaryExpression((BinaryExpressionSyntax)node);
            case SyntaxKind.NameExpression:
                return EvaluateNameExpression((NameExpressionSyntax)node);
            case SyntaxKind.CellReferenceExpression:
                return EvaluateCellReference((CellExpressionSyntax)node);
            case SyntaxKind.RangeReferenceExpression:
                return EvaluateRangeReference((RangeReferenceExpressionSyntax)node);
            case SyntaxKind.ParenthesizedExpression:
                return EvaluateParenthesizedExpression((ParenthesizedExpressionSyntax)node);
            case SyntaxKind.UnaryExpression:
                return EvaluateUnaryExpression((UnaryExpressionSyntax)node);
            case SyntaxKind.FunctionCallExpression:
                return EvaluateFunctionExpression((FunctionCallExpressionSyntax)node);
            default:
                return new FormulaError(ErrorType.Na, $"Unknown expression {node.Kind}");
        }
    }

    private object EvaluateFunctionExpression(FunctionCallExpressionSyntax node)
    {
        if (!_environment.FunctionExists(node.Identifier.Text))
            return new FormulaError(ErrorType.Name, $"Function {node.Identifier.Text} not found");

        var func = _environment.GetFunction(node.Identifier.Text);
        var nArgsProvided = node.Args.Count();

        if (nArgsProvided < func.MinArity ||
            nArgsProvided > func.MaxArity)
        {
            return new FormulaError(ErrorType.Na, "Incorrect number of function arguments");
        }

        int paramIndex = 0;
        int argIndex = 0;

        List<object> convertedArgs = new List<object>();

        while (paramIndex < func.Parameters.Count &&
               argIndex < nArgsProvided)
        {
            var param = func.Parameters[paramIndex];
            var arg = Evaluate(node.Args[argIndex]);

            if (arg is FormulaError &&
                !func.AcceptsErrors)
                return arg;

            var converted = _converter.ConvertTo(param.ParameterType, arg);
            if (converted is FormulaError &&
                !func.AcceptsErrors)
            {
                return converted;
            }

            convertedArgs.Add(converted);
            if (isConsumable(param))
                paramIndex++;
            argIndex++;
        }

        return func.Call(convertedArgs);
    }

    private bool isConsumable(Parameter param)
    {
        return !param.IsRepeating;
    }

    private object EvaluateRangeReference(RangeReferenceExpressionSyntax node)
    {
        var start = node.Reference.Start;
        var end = node.Reference.End;

        if (start.Kind == ReferenceKind.Cell && end.Kind == ReferenceKind.Cell)
        {
            var cellStart = (CellReference)start;
            var cellEnd = (CellReference)end;
            return _environment.GetRange(cellStart.Row.RowNumber,
                                         cellEnd.Row.RowNumber,
                                         cellStart.Col.ColNumber,
                                         cellEnd.Col.ColNumber);
        }

        if (start.Kind == ReferenceKind.Column && end.Kind == ReferenceKind.Column)
        {
            var colStart = (ColReference)start;
            var colEnd = (ColReference)end;
            return _environment.GetColRange(colStart, colEnd);
        }

        if (start.Kind == ReferenceKind.Row && end.Kind == ReferenceKind.Row)
        {
            var rowStart = (RowReference)start;
            var rowEnd = (RowReference)end;
            return _environment.GetRowRange(rowStart, rowEnd);
        }

        return new FormulaError(ErrorType.Name, $"Range could not be evaluated");
    }

    private object EvaluateParenthesizedExpression(ParenthesizedExpressionSyntax node)
    {
        return Evaluate(node.ExpressionSyntax);
    }

    private object EvaluateCellReference(CellExpressionSyntax node)
    {
        var cell = _environment.GetCell(node.CellReference.Row.RowNumber, node.CellReference.Col.ColNumber);
        return cell;
    }

    private object EvaluateLiteralExpression(LiteralExpressionSyntax syntax)
    {
        return syntax.Value;
    }

    private object EvaluateNameExpression(NameExpressionSyntax syntax)
    {
        if (_environment.VariableExists(syntax.IdentifierToken.Text))
            return _environment.GetVariable(syntax.IdentifierToken.Text);

        return new FormulaError(ErrorType.Ref, $"Variable {syntax.IdentifierToken.Text} does not exist.");
    }

    private object EvaluateUnaryExpression(UnaryExpressionSyntax syntax)
    {
        var operand = Evaluate(syntax.Operand);

        if (operand is FormulaError)
            return operand;

        switch (syntax.OperatorToken.Kind)
        {
            case SyntaxKind.MinusToken:
                if (operand is double d)
                    return -d;
                else
                    return InvalidUnaryExpression(syntax, operand);
        }

        return InvalidUnaryExpression(syntax, operand);
    }

    private FormulaError InvalidUnaryExpression(UnaryExpressionSyntax syntax, object operand)
    {
        return new FormulaError(ErrorType.Na,
                                $"Invalid operation {operand.ToString()} on {syntax.OperatorToken.Text}");
    }

    private object EvalueBinaryExpression(BinaryExpressionSyntax syntax)
    {
        var left = Evaluate(syntax.Left);
        var right = Evaluate(syntax.Right);
        if (left is FormulaError)
            return left;
        if (right is FormulaError)
            return right;

        if (left is ICell l)
            left = l.Value;
        if (right is ICell r)
            right = r.Value;

        if (IsValid(left, right, syntax.OperatorToken))
        {
            switch (syntax.OperatorToken.Kind)
            {
                case SyntaxKind.PlusToken:
                    return (double)left + (double)right;
                case SyntaxKind.MinusToken:
                    return (double)left - (double)right;
                case SyntaxKind.StarToken:
                    return (double)left * (double)right;
                case SyntaxKind.SlashToken:
                    return (double)left / (double)right;
                case SyntaxKind.EqualsToken:
                    return left.GetType() == right.GetType() &&
                           ((IComparable)left).Equals(right);
                case SyntaxKind.NotEqualsToken:
                    return left != right;
                case SyntaxKind.LessThanToken:
                    return ((IComparable)left).CompareTo(right) < 0;
                case SyntaxKind.GreaterThanToken:
                    return ((IComparable)left).CompareTo(right) > 0;
                case SyntaxKind.GreaterThanEqualToToken:
                    return ((IComparable)left).CompareTo(right) >= 0;
                case SyntaxKind.LessThanEqualToToken:
                    return ((IComparable)left).CompareTo(right) <= 0;
                default:
                    return new FormulaError(ErrorType.Na, $"Unknown operator {syntax.OperatorToken.Kind}");
            }
        }

        return new FormulaError(ErrorType.Value, "Invalid input to binary expression");
    }

    private bool IsValid(object left, object right, SyntaxToken binaryExpressionOperatorToken)
    {
        // TODO replace with some other pattern so we can more easily handle 
        // operators that operate on different types e.g < can operated on strings and numbers
        switch (binaryExpressionOperatorToken.Kind)
        {
            case SyntaxKind.PlusToken:
            case SyntaxKind.StarToken:
            case SyntaxKind.SlashToken:
            case SyntaxKind.MinusToken:
                return (left as double?) != null && (right as double?) != null;
            case SyntaxKind.GreaterThanToken:
            case SyntaxKind.GreaterThanEqualToToken:
            case SyntaxKind.LessThanToken:
            case SyntaxKind.LessThanEqualToToken:
                return (left as IComparable) != null &&
                       (right as IComparable) != null &&
                       left.GetType() == right.GetType();
            case SyntaxKind.EqualsToken:
                return (left as IComparable) != null &&
                       (right as IComparable) != null;
            default:
                return false;
        }
    }
}