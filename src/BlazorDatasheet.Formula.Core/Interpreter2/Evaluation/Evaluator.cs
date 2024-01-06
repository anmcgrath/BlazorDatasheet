using BlazorDatasheet.DataStructures.References;
using BlazorDatasheet.Formula.Core.Interpreter.Functions;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using BlazorDatasheet.Formula.Core.Interpreter.Syntax;
using BlazorDatasheet.Formula.Core.Interpreter2.Parsing;
using SyntaxTree = BlazorDatasheet.Formula.Core.Interpreter2.Parsing.SyntaxTree;

namespace BlazorDatasheet.Formula.Core.Interpreter2.Evaluation;

public class Evaluator
{
    private readonly ParameterConverter _parameterConverter;
    private readonly BinaryOpEvaluator _bOp;
    private readonly UnaryOpEvaluator _uOp;
    private readonly IEnvironment _environment;

    public Evaluator(IEnvironment environment)
    {
        _environment = environment;
        var cellValueCoercer = new CellValueCoercer(_environment);
        _parameterConverter = new ParameterConverter(_environment, cellValueCoercer);
        _bOp = new BinaryOpEvaluator(cellValueCoercer);
        _uOp = new UnaryOpEvaluator(cellValueCoercer);
    }

    public CellValue Evaluate(SyntaxTree tree)
    {
        if (tree.Errors.Any())
            return CellValue.Error(ErrorType.Na);
        var result = EvaluateExpression(tree.Root);

        // If we haven't resolved references yet, do that.
        if (result.ValueType == CellValueType.Reference)
        {
            var r = (Reference)result.Data!;
            if (r.Kind == ReferenceKind.Cell)
            {
                var c = (CellReference)r;
                return _environment.GetCellValue(c.Row.RowNumber, c.Col.ColNumber);
            }
            else if (r.Kind == ReferenceKind.Range)
            {
                return CellValue.Array(_environment
                    .GetRangeValues(r));
            }
        }

        // otherwise return the calculated result.
        return result;
    }

    public CellValue EvaluateExpression(Expression expression)
    {
        switch (expression.Kind)
        {
            case NodeKind.Literal:
                return EvaluateLiteral((LiteralExpression)expression);
            case NodeKind.BinaryOperation:
                return EvaluateBinaryExpression((BinaryOperationExpression)expression);
            case NodeKind.ParenthesizedExpression:
                return EvaluateParenthesizedExpression((ParenthesizedExpression)expression);
            case NodeKind.FunctionCall:
                return EvaluateFunctionCall((FunctionExpression)expression);
            case NodeKind.Range:
                return EvaluateReferenceExpression((ReferenceExpression)expression);
            case NodeKind.UnaryOperation:
                return EvaluateUnaryExpression((UnaryOperatorExpression)expression);
            case NodeKind.ArrayConstant:
                return EvaluateArrayConstantExpression((ArrayConstantExpression)expression);
            case NodeKind.Name:
                return EvaluateNamedExpression((NameExpression)expression);
        }

        return CellValue.Error(new FormulaError(ErrorType.Na,
            $"Cannot evaluate expression {expression.ToExpressionText()}"));
    }

    private CellValue EvaluateNamedExpression(NameExpression expression)
    {
        if (!_environment.VariableExists(expression.NameToken.Value))
            return CellValue.Error(ErrorType.Ref);
        return _environment.GetVariable(expression.NameToken.Value);
    }

    private CellValue EvaluateArrayConstantExpression(ArrayConstantExpression expression)
    {
        var values = new CellValue[expression.Rows.Count][];
        for (var i = 0; i < expression.Rows.Count; i++)
        {
            var row = expression.Rows[i];
            values[i] = new CellValue[row.Count];
            for (int j = 0; j < row.Count; j++)
            {
                values[i][j] = EvaluateLiteral(row[j]);
            }
        }

        return CellValue.Array(values);
    }

    private CellValue EvaluateUnaryExpression(UnaryOperatorExpression expression)
    {
        var val = EvaluateExpression(expression.Expression);
        return _uOp.Evaluate(expression.OperatorToken.Tag, val);
    }

    private CellValue EvaluateReferenceExpression(ReferenceExpression expression)
    {
        //TODO check it's valid (inside sheet)
        return CellValue.Reference(expression.Reference);
    }

    private CellValue EvaluateFunctionCall(FunctionExpression node)
    {
        var id = node.FunctionToken.Value;
        if (!_environment.FunctionExists(id))
            return CellValue.Error(new FormulaError(ErrorType.Name, $"Function {id} not found"));

        var func = _environment.GetFunctionDefinition(id);
        var nArgsProvided = node.Args.Count();

        var paramDefinitions = func.GetParameterDefinitions();

        if (nArgsProvided < MinArity(paramDefinitions) ||
            nArgsProvided > MaxArity(paramDefinitions))
        {
            return CellValue.Error(ErrorType.Na, "Incorrect number of function arguments");
        }

        int paramIndex = 0;
        int argIndex = 0;

        CellValue[] convertedArgs = new CellValue[nArgsProvided];

        while (paramIndex < paramDefinitions.Length &&
               argIndex < nArgsProvided)
        {
            var paramDefinition = paramDefinitions[paramIndex];
            var arg = EvaluateExpression(node.Args[argIndex]);

            if (arg.IsError() && !func.AcceptsErrors)
                return arg;

            convertedArgs[argIndex] = _parameterConverter.ConvertVal(arg, paramDefinition);

            if (IsConsumable(paramDefinition))
            {
                paramIndex++;
            }

            argIndex++;
        }

        return new CellValue(func.Call(convertedArgs, new FunctionCallMetaData(paramDefinitions)),
            CellValueType.Number);
    }

    private bool IsConsumable(ParameterDefinition param)
    {
        return !param.IsRepeating;
    }

    private int MaxArity(ParameterDefinition[] parameterDefinitions)
    {
        return parameterDefinitions.Last().IsRepeating ? 128 : parameterDefinitions.Length;
    }

    private int MinArity(ParameterDefinition[] parameterDefinitions)
    {
        return parameterDefinitions.Count(x => x.Requirement == ParameterRequirement.Required);
    }

    private CellValue EvaluateBinaryExpression(BinaryOperationExpression expression)
    {
        var left = EvaluateExpression(expression.Left);
        var right = EvaluateExpression(expression.Right);
        return _bOp.Evaluate(left, expression.OpToken.Tag, right);
    }

    private CellValue EvaluateParenthesizedExpression(ParenthesizedExpression expression) =>
        EvaluateExpression(expression.Expression);

    private CellValue EvaluateLiteral(LiteralExpression expression)
    {
        return expression.Value;
    }
}