using BlazorDatasheet.Formula.Core.Interpreter.Parsing;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using SyntaxTree = BlazorDatasheet.Formula.Core.Interpreter.Parsing.SyntaxTree;

namespace BlazorDatasheet.Formula.Core.Interpreter.Evaluation;

public class Evaluator
{
    private readonly ParameterConverter _parameterConverter;
    private readonly BinaryOpEvaluator _bOp;
    private readonly UnaryOpEvaluator _uOp;
    private readonly IEnvironment _environment;

    private readonly record struct EvalContext(
        FormulaEvaluationOptions Options,
        FormulaExecutionContext ExecutionContext);

    public Evaluator(IEnvironment environment)
    {
        _environment = environment;
        var cellValueCoercer = new CellValueCoercer(_environment);
        _parameterConverter = new ParameterConverter(_environment, cellValueCoercer);
        _bOp = new BinaryOpEvaluator(cellValueCoercer, _environment);
        _uOp = new UnaryOpEvaluator(cellValueCoercer);
    }

    public CellValue Evaluate(CellFormula cellFormula, FormulaExecutionContext? executionContext = null,
        FormulaEvaluationOptions? options = null)
    {
        var ctx = new EvalContext(
            options ?? FormulaEvaluationOptions.Default,
            executionContext ?? new FormulaExecutionContext());
        return DoEvaluate(cellFormula, ctx);
    }

    private CellValue DoEvaluate(CellFormula formula, EvalContext ctx)
    {
        if (ctx.ExecutionContext.IsExecuting(formula))
            return CellValue.Error(ErrorType.Circular);

        ctx.ExecutionContext.SetExecuting(formula);
        return EvaluateTree(formula.ExpressionTree, ctx);
    }

    /// <summary>
    /// Evaluates a syntax tree
    /// </summary>
    internal CellValue Evaluate(SyntaxTree tree) =>
        EvaluateTree(tree, new EvalContext(FormulaEvaluationOptions.Default, new FormulaExecutionContext()));

    private CellValue EvaluateTree(SyntaxTree tree, EvalContext ctx)
    {
        if (tree.Errors.Count > 0)
            return CellValue.Error(ErrorType.Na);

        var result = EvaluateExpression(tree.Root, ctx);
        // If we haven't resolved references yet, do that
        // We do this at the end of evaluation so that we can pass
        // references to functions.
        if (!ctx.Options.DoNotResolveDependencies && result.ValueType == CellValueType.Reference)
        {
            var r = (Reference)result.Data!;
            if (r.Kind == ReferenceKind.Cell)
                return _environment.GetCellValue(((CellReference)r).RowIndex, ((CellReference)r).ColIndex, r.SheetName);
            else if (r.Kind == ReferenceKind.Range)
                return CellValue.Array(_environment.GetRangeValues(r));
        }

        return result;
    }

    private CellValue EvaluateExpression(Expression expression, EvalContext ctx)
    {
        switch (expression.Kind)
        {
            case NodeKind.Literal:
                return EvaluateLiteral((LiteralExpression)expression);
            case NodeKind.BinaryOperation:
                return EvaluateBinaryExpression((BinaryOperationExpression)expression, ctx);
            case NodeKind.ParenthesizedExpression:
                return EvaluateParenthesizedExpression((ParenthesizedExpression)expression, ctx);
            case NodeKind.FunctionCall:
                return EvaluateFunctionCall((FunctionExpression)expression, ctx);
            case NodeKind.Range:
                return EvaluateReferenceExpression((ReferenceExpression)expression, ctx);
            case NodeKind.UnaryOperation:
                return EvaluateUnaryExpression((UnaryOperatorExpression)expression, ctx);
            case NodeKind.ArrayConstant:
                return EvaluateArrayConstantExpression((ArrayConstantExpression)expression);
            case NodeKind.Name:
                return EvaluateNamedExpression((VariableExpression)expression);
            case NodeKind.Error:
                return CellValue.Error(((ErrorExpression)expression).ErrorType);
        }

        return CellValue.Error(new FormulaError(ErrorType.Na,
            $"Cannot evaluate expression {expression.ToExpressionText()}"));
    }

    private CellValue EvaluateNamedExpression(VariableExpression expression)
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

    private CellValue EvaluateUnaryExpression(UnaryOperatorExpression expression, EvalContext ctx)
    {
        var val = EvaluateExpression(expression.Expression, ctx);
        return _uOp.Evaluate(expression.OperatorToken.Tag, val);
    }

    private CellValue EvaluateReferenceExpression(ReferenceExpression expression, EvalContext ctx)
    {
        //TODO check it's valid (inside sheet)
        if (expression.Reference.IsInvalid)
            return CellValue.Error(ErrorType.Ref);

        if (expression.Reference.Kind == ReferenceKind.Cell)
            return EvaluateCellReference((CellReference)expression.Reference, ctx);

        if (expression.Reference.Kind == ReferenceKind.Range)
            return EvaluateRangeReference((RangeReference)expression.Reference);

        // we currently don't handle array values...
        return CellValue.Reference(expression.Reference);
    }

    private CellValue EvaluateCellReference(CellReference cellReference, EvalContext ctx)
    {
        if (ctx.Options.DoNotResolveDependencies)
            return CellValue.Reference(cellReference);

        if (!ctx.ExecutionContext.IsInSccGroup(cellReference))
            return CellValue.Reference(cellReference);

        var formula = _environment.GetFormula(cellReference.RowIndex, cellReference.ColIndex, cellReference.SheetName);
        if (formula == null)
            return CellValue.Reference(cellReference);

        if (ctx.ExecutionContext.TryGetExecutedValue(formula, out var result))
            return result;

        return DoEvaluate(formula, ctx);
    }

    private CellValue EvaluateRangeReference(RangeReference reference)
    {
        return CellValue.Reference(reference);
    }

    private CellValue EvaluateFunctionCall(FunctionExpression node, EvalContext ctx)
    {
        var id = node.FunctionToken.Value;
        if (!node.FunctionExists)
            return CellValue.Error(new FormulaError(ErrorType.Name, $"Function {id} not found"));

        var func = node.Function!;
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
            var arg = EvaluateExpression(node.Args[argIndex], ctx);

            convertedArgs[argIndex] = _parameterConverter.ConvertVal(arg, paramDefinition.Type);

            if (convertedArgs[argIndex].IsError() && !func.AcceptsErrors)
                return convertedArgs[argIndex];

            if (IsConsumable(paramDefinition))
                paramIndex++;

            argIndex++;
        }

        var funcResult = func.Call(convertedArgs, new FunctionCallMetaData(paramDefinitions));
        return funcResult;
    }

    private bool IsConsumable(ParameterDefinition param)
    {
        return !param.IsRepeating;
    }

    private int MaxArity(ParameterDefinition[] parameterDefinitions)
    {
        if (parameterDefinitions.Length == 0)
            return 0;
        return parameterDefinitions.Last().IsRepeating ? 128 : parameterDefinitions.Length;
    }

    private int MinArity(ParameterDefinition[] parameterDefinitions)
    {
        return parameterDefinitions.Count(x => x.Requirement == ParameterRequirement.Required);
    }

    private CellValue EvaluateBinaryExpression(BinaryOperationExpression expression, EvalContext ctx)
    {
        var left = EvaluateExpression(expression.Left, ctx);
        var right = EvaluateExpression(expression.Right, ctx);

        return _bOp.Evaluate(left, expression.OpToken.Tag, right);
    }

    private CellValue EvaluateParenthesizedExpression(ParenthesizedExpression expression, EvalContext ctx) =>
        EvaluateExpression(expression.Expression, ctx);

    private CellValue EvaluateLiteral(LiteralExpression expression)
    {
        return expression.Value;
    }
}
