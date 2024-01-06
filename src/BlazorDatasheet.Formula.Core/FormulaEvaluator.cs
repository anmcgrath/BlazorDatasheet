using BlazorDatasheet.DataStructures.References;
using BlazorDatasheet.Formula.Core.Interpreter.Functions;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using BlazorDatasheet.Formula.Core.Interpreter.Syntax;

namespace BlazorDatasheet.Formula.Core;

public class FormulaEvaluator
{
    private readonly IEnvironment _environment;
    private readonly ParameterTypeConverter _typeConverter;

    /// <summary>
    /// The row address of the currently evaluated formula. If null, then the formula is not associated with a row/col.
    /// </summary>
    private int? _currentRow;

    /// <summary>
    /// The column address of the currently evaluated formula. If null, then the formula is not associated with a row/col.
    /// </summary>
    private int? _currentCol;

    public FormulaEvaluator(IEnvironment environment)
    {
        _environment = environment;
        _typeConverter = new ParameterTypeConverter(environment);
    }

    public object Evaluate(SyntaxTree tree)
    {
        if (tree.Diagnostics.Any())
            return new FormulaError(ErrorType.Na);
        return Evaluate(tree.Root);
    }

    /// <summary>
    /// Evaluates the formula at a given position.
    /// </summary>
    /// <param name="cellFormula">The formula to evaluate</param>
    /// <param name="row">The row of the formula. Required to evaluate relative references.</param>
    /// <param name="col">The column of the formula. Required to evaluate relative reference.</param>
    /// <returns></returns>
    public object Evaluate(CellFormula cellFormula, int? row, int? col)
    {
        _currentCol = col;
        _currentRow = row;
        return Evaluate(cellFormula.ExpressionTree);
    }

    /// <summary>
    /// Evaluate the formula.
    /// </summary>
    /// <param name="cellFormula">The formula to evaluate</param>
    /// <returns></returns>
    public object? Evaluate(CellFormula cellFormula)
    {
        _currentCol = null;
        _currentRow = null;
        var result = Evaluate(cellFormula.ExpressionTree);
        // The only case we want to do any conversion on here is if we are
        // evaluating a formula and end up with a cell address as the result.
        // In that case we want to get the cell's value
        if (result is CellAddress addr)
            return _environment.GetCellValue(addr.RowStart, addr.ColStart).Data;

        return result;
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
            case SyntaxKind.ArrayConstantExpression:
                return EvaluateArrayConstantExpression((ArrayConstantExpressionSyntax)node);
            case SyntaxKind.FunctionCallExpression:
                return EvaluateFunctionExpression((FunctionCallExpressionSyntax)node);
            default:
                return new FormulaError(ErrorType.Na, $"Unknown expression {node.Kind}");
        }
    }

    private CellValue[][] EvaluateArrayConstantExpression(ArrayConstantExpressionSyntax node)
    {
        var result = new CellValue[node.Rows.Count][];
        for (int i = 0; i < node.Rows.Count; i++)
        {
            result[i] = node.Rows[i].Select(x =>
                    new CellValue(EvaluateLiteralExpression((LiteralExpressionSyntax)x)))
                .ToArray();
        }

        return result;
    }

    private int MaxArity(ParameterDefinition[] parameterDefinitions)
    {
        return parameterDefinitions.Last().IsRepeating ? 128 : parameterDefinitions.Length;
    }

    private int MinArity(ParameterDefinition[] parameterDefinitions)
    {
        return parameterDefinitions.Count(x => x.Requirement == ParameterRequirement.Required);
    }

    private object? EvaluateFunctionExpression(FunctionCallExpressionSyntax node)
    {
        if (!_environment.FunctionExists(node.Identifier.Text))
            return new FormulaError(ErrorType.Name, $"Function {node.Identifier.Text} not found");

        var func = _environment.GetFunctionDefinition(node.Identifier.Text);
        var nArgsProvided = node.Args.Count();

        var paramDefinitions = func.GetParameterDefinitions();

        if (nArgsProvided < MinArity(paramDefinitions) ||
            nArgsProvided > MaxArity(paramDefinitions))
        {
            return new FormulaError(ErrorType.Na, "Incorrect number of function arguments");
        }

        int paramIndex = 0;
        int argIndex = 0;

        CellValue[] convertedArgs = new CellValue[nArgsProvided];

        while (paramIndex < paramDefinitions.Length &&
               argIndex < nArgsProvided)
        {
            var paramDefinition = paramDefinitions[paramIndex];
            var arg = Evaluate(node.Args[argIndex]);

            if (arg is FormulaError &&
                !func.AcceptsErrors)
                return arg;

            convertedArgs[argIndex] = _typeConverter.ConvertVal(arg, paramDefinition);

            if (IsConsumable(paramDefinition))
            {
                paramIndex++;
            }

            argIndex++;
        }

        return func.Call(convertedArgs, new FunctionCallMetaData(paramDefinitions));
    }

    private bool IsConsumable(ParameterDefinition param)
    {
        return !param.IsRepeating;
    }

    private object EvaluateRangeReference(RangeReferenceExpressionSyntax node)
    {
        if (node.Reference.IsRelativeReference &&
            (_currentCol == null || _currentRow == null))
        {
            return new FormulaError(ErrorType.Ref,
                "Relative reference used but formula is not defined on a row/column.");
        }

        var start = node.Reference.Start;
        var end = node.Reference.End;


        if (start.Kind == ReferenceKind.Cell && end.Kind == ReferenceKind.Cell)
        {
            return ToRangeAddress(start, end);
        }

        if (start.Kind == ReferenceKind.Column && end.Kind == ReferenceKind.Column)
        {
            return ToColumnAddress((ColReference)start, (ColReference)end);
        }

        if (start.Kind == ReferenceKind.Row && end.Kind == ReferenceKind.Row)
        {
            return ToRowAddress((RowReference)start, (RowReference)end);
        }

        return new FormulaError(ErrorType.Name, "Range could not be evaluated");
    }

    private object ToRowAddress(RowReference start, RowReference end)
    {
        var colStart = start.IsRelativeReference
            ? start.RowNumber + _currentRow!.Value
            : start.RowNumber;
        var colEnd = end.IsRelativeReference
            ? end.RowNumber + _currentRow!.Value
            : end.RowNumber;

        return new RowAddress(Math.Min(colStart, colEnd), Math.Max(colStart, colEnd));
    }

    private ColumnAddress ToColumnAddress(ColReference colStartRef, ColReference colEndRef)
    {
        var colStart = colStartRef.IsRelativeReference
            ? colStartRef.ColNumber + _currentCol!.Value
            : colStartRef.ColNumber;
        var colEnd = colEndRef.IsRelativeReference
            ? colEndRef.ColNumber + _currentCol!.Value
            : colEndRef.ColNumber;

        return new ColumnAddress(Math.Min(colStart, colEnd), Math.Max(colStart, colEnd));
    }

    private RangeAddress ToRangeAddress(Reference referenceStart, Reference referenceEnd)
    {
        var cellStart = (CellReference)referenceStart;
        var cellEnd = (CellReference)referenceEnd;
        var colStart = cellStart.IsRelativeReference
            ? cellStart.Col.ColNumber + _currentCol!.Value
            : cellStart.Col.ColNumber;
        var colEnd = cellEnd.IsRelativeReference
            ? cellEnd.Col.ColNumber + _currentCol!.Value
            : cellEnd.Col.ColNumber;
        var rowStart = cellStart.IsRelativeReference
            ? cellStart.Row.RowNumber + _currentRow!.Value
            : cellStart.Row.RowNumber;
        var rowEnd = cellEnd.IsRelativeReference
            ? cellEnd.Row.RowNumber + _currentRow!.Value
            : cellEnd.Row.RowNumber;

        return new RangeAddress(Math.Min(rowStart, rowEnd), Math.Max(rowStart, rowEnd), Math.Min(colStart, colEnd),
            Math.Max(colStart, colEnd));
    }

    private object EvaluateParenthesizedExpression(ParenthesizedExpressionSyntax node)
    {
        return Evaluate(node.ExpressionSyntax);
    }

    private object EvaluateCellReference(CellExpressionSyntax node)
    {
        if (node.CellReference.IsRelativeReference &&
            (_currentCol == null || _currentRow == null))
        {
            return new FormulaError(ErrorType.Ref,
                "Relative reference used but formula is not defined on a row/column.");
        }

        var row = node.CellReference.IsRelativeReference
            ? _currentRow!.Value + node.CellReference.Row.RowNumber
            : node.CellReference.Row.RowNumber;

        var col = node.CellReference.IsRelativeReference
            ? _currentCol!.Value + node.CellReference.Col.ColNumber
            : node.CellReference.Col.ColNumber;

        return new CellAddress(row, col);
    }

    private object EvaluateLiteralExpression(LiteralExpressionSyntax syntax)
    {
        return syntax.Value;
    }

    private object EvaluateNameExpression(NameExpressionSyntax syntax)
    {
        if (_environment.VariableExists(syntax.IdentifierToken.Text))
            return _environment.GetVariable(syntax.IdentifierToken.Text).Data!;

        return new FormulaError(ErrorType.Ref, $"Variable {syntax.IdentifierToken.Text} does not exist.");
    }

    private object EvaluateUnaryExpression(UnaryExpressionSyntax syntax)
    {
        var operand = Evaluate(syntax.Operand);

        if (operand is CellAddress addr)
            operand = _environment.GetCellValue(addr.RowStart, addr.ColStart).Data;

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

        if (left is CellAddress addrLeft)
            left = _environment.GetCellValue(addrLeft.RowStart, addrLeft.ColStart).Data;

        if (right is CellAddress addrRight)
            right = _environment.GetCellValue(addrRight.RowStart, addrRight.ColStart).Data;

        if (IsValid(left, right, syntax.OperatorToken))
        {
            switch (syntax.OperatorToken.Kind)
            {
                case SyntaxKind.PlusToken:
                    return ConvertToDouble(left) + ConvertToDouble(right);
                case SyntaxKind.MinusToken:
                    return ConvertToDouble(left) - ConvertToDouble(right);
                case SyntaxKind.StarToken:
                    return ConvertToDouble(left) * ConvertToDouble(right);
                case SyntaxKind.SlashToken:
                {
                    var rightDouble = ConvertToDouble(right);
                    if (rightDouble == 0)
                        return new FormulaError(ErrorType.Div0);

                    return ConvertToDouble(left) / ConvertToDouble(right);
                }
                case SyntaxKind.EqualsToken:
                    if (left == null && right == null)
                        return true;
                    if (left == null || right == null)
                        return false;
                    return left.GetType() == right.GetType() &&
                           ((IComparable)left).Equals(right);
                case SyntaxKind.NotEqualsToken:
                    if (left == null || right == null)
                        return left == right;
                    return left.GetType() != right.GetType() ||
                           !((IComparable)left).Equals(right);
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

    private bool IsValid(object? left, object? right, SyntaxToken binaryExpressionOperatorToken)
    {
        // TODO replace with some other pattern so we can more easily handle 
        // operators that operate on different types e.g < can operated on strings and numbers
        switch (binaryExpressionOperatorToken.Kind)
        {
            case SyntaxKind.PlusToken:
            case SyntaxKind.StarToken:
            case SyntaxKind.SlashToken:
            case SyntaxKind.MinusToken:
                return ConvertsToDouble(left) && ConvertsToDouble(right);
            case SyntaxKind.GreaterThanToken:
            case SyntaxKind.GreaterThanEqualToToken:
            case SyntaxKind.LessThanToken:
            case SyntaxKind.LessThanEqualToToken:
                return (left as IComparable) != null &&
                       (right as IComparable) != null &&
                       left.GetType() == right.GetType();
            case SyntaxKind.EqualsToken:
            case SyntaxKind.NotEqualsToken:
                return (left == null || right == null) ||
                       ((left as IComparable) != null &&
                        (right as IComparable) != null);
            default:
                return false;
        }
    }

    private double ConvertToDouble(object? value)
    {
        if (value == null)
            return 0;

        return Convert.ToDouble(value);
    }

    private bool ConvertsToDouble(object? value)
    {
        if (value == null)
            return true;

        if (value is double or decimal or int or float)
            return true;

        if (value is bool)
            return true;

        if (double.TryParse(value.ToString(), out var temp))
            return true;

        return false;
    }
}