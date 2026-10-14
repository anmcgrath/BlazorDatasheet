using System.Buffers;
using BlazorDatasheet.Formula.Core.Interpreter.Addresses;
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
        FormulaExecutionContext ExecutionContext,
        FormulaCallerInfo? Caller);

    public Evaluator(IEnvironment environment)
    {
        _environment = environment;
        var cellValueCoercer = new CellValueCoercer(_environment);
        _parameterConverter = new ParameterConverter(_environment, cellValueCoercer);
        _bOp = new BinaryOpEvaluator(cellValueCoercer, _environment);
        _uOp = new UnaryOpEvaluator(cellValueCoercer, _environment);
    }

    public CellValue Evaluate(CellFormula cellFormula, FormulaExecutionContext? executionContext = null,
        FormulaEvaluationOptions? options = null,
        FormulaCallerInfo? caller = null)
    {
        var ctx = new EvalContext(
            options ?? FormulaEvaluationOptions.Default,
            executionContext ?? new FormulaExecutionContext(),
            caller);
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
        EvaluateTree(tree, new EvalContext(FormulaEvaluationOptions.Default, new FormulaExecutionContext(), null));

    private CellValue EvaluateTree(SyntaxTree tree, EvalContext ctx)
    {
        if (tree.Errors.Count > 0)
            return CellValue.Error(ErrorType.Na);

        var result = EvaluateExpression(tree.Root, ctx);
        // Keep references available to functions, resolving the final result only when requested.
        if (!ctx.Options.DoNotResolveDependencies)
            result = ResolveReference(result);

        return NormalizeArrayResult(result);
    }

    private CellValue ResolveReference(CellValue value)
    {
        if (value.ValueType != CellValueType.Reference)
            return value;

        var reference = (Reference)value.Data!;
        return reference.Kind switch
        {
            ReferenceKind.Cell => _environment.GetCellValue(((CellReference)reference).RowIndex,
                ((CellReference)reference).ColIndex, reference.SheetName),
            ReferenceKind.Range => CellValue.Array(_environment.GetRangeValues(reference)),
            _ => CellValue.Error(ErrorType.Ref)
        };
    }

    /// <summary>
    /// Array results are rectangular and contain values, never references or nested collections.
    /// Normalize before another function consumes the result as well as before it reaches the sheet.
    /// Copy only rows containing references so function-owned arrays are not modified.
    /// </summary>
    private CellValue NormalizeArrayResult(CellValue value)
    {
        if (!value.IsArray())
            return value;

        var source = (CellValue[][])value.Data!;
        var columns = source.Length == 0 ? 0 : source[0]?.Length ?? -1;
        CellValue[][]? result = null;
        for (var r = 0; r < source.Length; r++)
        {
            var row = source[r];
            if (row == null || row.Length != columns)
                return CellValue.Error(ErrorType.Calc, "Array rows must have the same length");

            CellValue[]? resolvedRow = null;
            for (var c = 0; c < columns; c++)
            {
                var element = ResolveReference(row[c]);
                if (element.ValueType is CellValueType.Array or CellValueType.Sequence or CellValueType.Reference)
                    return CellValue.Error(ErrorType.Calc, "Nested arrays are not supported");

                if (row[c].ValueType == CellValueType.Reference)
                {
                    result ??= (CellValue[][])source.Clone();
                    resolvedRow ??= (CellValue[])row.Clone();
                    resolvedRow[c] = element;
                    result[r] = resolvedRow;
                }
            }
        }

        return result == null ? value : CellValue.Array(result);
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
                return NormalizeArrayResult(EvaluateFunctionCall((FunctionExpression)expression, ctx));
            case NodeKind.Range:
                return EvaluateReferenceExpression((ReferenceExpression)expression, ctx);
            case NodeKind.UnaryOperation:
                return EvaluateUnaryExpression((UnaryOperatorExpression)expression, ctx);
            case NodeKind.ArrayConstant:
                return EvaluateArrayConstantExpression((ArrayConstantExpression)expression);
            case NodeKind.Name:
                return NormalizeArrayResult(EvaluateNamedExpression((VariableExpression)expression, ctx));
            case NodeKind.Error:
                return CellValue.Error(((ErrorExpression)expression).ErrorType);
        }

        return CellValue.Error(new FormulaError(ErrorType.Na,
            $"Cannot evaluate expression {expression.ToExpressionText()}"));
    }

    private CellValue EvaluateNamedExpression(VariableExpression expression, EvalContext ctx)
    {
        var name = expression.NameToken.Value;
        if (!ctx.ExecutionContext.TryGetNamedFormula(name, out var formula))
        {
            if (_environment.TryGetVariable(name, out var value))
                return value;

            return CellValue.Error(ErrorType.Ref);
        }

        if (ctx.ExecutionContext.TryGetExecutedValue(formula, out var result))
            return result;

        return DoEvaluate(formula, new EvalContext(
            ctx.Options,
            ctx.ExecutionContext,
            null));
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

        var reference = expression.Reference;

        if (ctx.Options.ReferenceOffset is { } offset &&
            reference.Kind is ReferenceKind.Cell or ReferenceKind.Range or ReferenceKind.SpillRange &&
            reference.SheetName == offset.SheetName &&
            (offset.RowOffset != 0 || offset.ColOffset != 0))
        {
            reference = reference.Copy();
            reference.Shift(offset.RowOffset, offset.ColOffset);
            if (reference.Region.Top < 0 || reference.Region.Left < 0)
                return CellValue.Error(ErrorType.Ref);
        }

        if (reference.Kind == ReferenceKind.Cell)
            return EvaluateCellReference((CellReference)reference, ctx);

        if (reference.Kind == ReferenceKind.Range)
            return EvaluateRangeReference((RangeReference)reference);

        if (reference.Kind == ReferenceKind.SpillRange)
            return EvaluateSpillRangeReference((SpillRangeReference)reference);

        return CellValue.Reference(reference);
    }

    /// <summary>
    /// A1# stands for whatever the formula at A1 currently spills into, so it is looked up at
    /// evaluation time and handed on as an ordinary range reference.
    /// </summary>
    private CellValue EvaluateSpillRangeReference(SpillRangeReference reference)
    {
        var region = _environment.GetSpillRegion(reference.RowIndex, reference.ColIndex, reference.SheetName);
        if (region == null)
            return CellValue.Error(ErrorType.Ref);

        var rangeRef = new RangeReference(new CellAddress(region.Top, region.Left),
            new CellAddress(region.Bottom, region.Right));
        rangeRef.SetSheetName(reference.SheetName, explicitSheetName: true);
        return CellValue.Reference(rangeRef);
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

        return DoEvaluate(formula, new EvalContext(
            ctx.Options,
            ctx.ExecutionContext,
            new FormulaCallerInfo(cellReference.RowIndex, cellReference.ColIndex, cellReference.SheetName)));
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

        var func = node.FunctionDescriptor!;
        var nArgsProvided = node.Args.Count;
        var paramDefinitions = func.ParameterDefinitions;

        if (nArgsProvided < func.MinArity ||
            nArgsProvided > func.MaxArity)
        {
            return CellValue.Error(ErrorType.Na, "Incorrect number of function arguments");
        }

        var acceptsErrors = func.AcceptsErrors;
        var callMetaData = new FunctionCallMetaData(
            ctx.Caller?.RowIndex,
            ctx.Caller?.ColIndex,
            ctx.Caller?.SheetName);

        if (nArgsProvided == 0)
            return func.Invoke(Span<CellValue>.Empty, callMetaData);

        var rentedArgs = ArrayPool<CellValue>.Shared.Rent(nArgsProvided);
        var rentedLifted = ArrayPool<bool>.Shared.Rent(nArgsProvided);
        var args = rentedArgs.AsSpan(0, nArgsProvided);
        var lifted = rentedLifted.AsSpan(0, nArgsProvided);
        lifted.Clear();

        try
        {
            // A scalar parameter given an array or range lifts the function over each element.
            var anyLifted = false;
            for (var i = 0; i < nArgsProvided; i++)
            {
                args[i] = EvaluateExpression(node.Args[i], ctx);
                if (IsLiftableParameter(GetParameterDefinition(paramDefinitions, i)) &&
                    ArrayBroadcaster.IsArrayLike(args[i]))
                {
                    args[i] = ArrayBroadcaster.Materialize(args[i], _environment);
                    // An array-returning function can consume a singleton scalar argument once,
                    // but lifting it over multiple elements would create nested arrays.
                    if (func.ReturnShape == ReturnShape.Array)
                    {
                        if (ArrayBroadcaster.Shape(args[i]) != (1, 1))
                            return CellValue.Error(ErrorType.Calc, "Cannot lift an array-returning function");
                        args[i] = ArrayBroadcaster.ElementAt(args[i], 0, 0);
                        continue;
                    }
                    lifted[i] = true;
                    anyLifted = true;
                }
            }

            if (!anyLifted)
            {
                for (var i = 0; i < nArgsProvided; i++)
                {
                    args[i] = _parameterConverter.ConvertVal(args[i], GetParameterDefinition(paramDefinitions, i).Type);
                    if (args[i].IsError() && !acceptsErrors)
                        return args[i];
                }

                return func.Invoke(args, callMetaData);
            }

            return EvaluateLiftedFunctionCall(func, paramDefinitions, args, lifted, callMetaData);
        }
        finally
        {
            ArrayPool<CellValue>.Shared.Return(rentedArgs, clearArray: true);
            ArrayPool<bool>.Shared.Return(rentedLifted);
        }
    }

    /// <summary>
    /// Calls the function once per element of the broadcast shape of the lifted arguments, returning
    /// the array of results. Arguments that are not lifted are converted once and passed whole.
    /// </summary>
    private CellValue EvaluateLiftedFunctionCall(FunctionDescriptor func, ParameterDefinition[] paramDefinitions,
        Span<CellValue> args, Span<bool> lifted, FunctionCallMetaData callMetaData)
    {
        var acceptsErrors = func.AcceptsErrors;
        var nArgs = args.Length;

        // an untyped argument given an array alongside a lifted one is lifted with it, so that
        // IF(A1:A3>1, B1:B3, 0) picks a value per row rather than returning the whole array.
        for (var i = 0; i < nArgs; i++)
        {
            var parameter = GetParameterDefinition(paramDefinitions, i);
            if (!lifted[i] &&
                parameter.Type == ParameterType.Any && parameter.Shape != ParameterShape.Array &&
                ArrayBroadcaster.IsArrayLike(args[i]))
            {
                args[i] = ArrayBroadcaster.Materialize(args[i], _environment);
                lifted[i] = true;
            }
        }

        var rows = 0;
        var cols = 0;
        for (var i = 0; i < nArgs; i++)
        {
            if (!lifted[i])
            {
                args[i] = _parameterConverter.ConvertVal(args[i], GetParameterDefinition(paramDefinitions, i).Type);
                if (args[i].IsError() && !acceptsErrors)
                    return args[i];
                continue;
            }

            var (r, c) = ArrayBroadcaster.Shape(args[i]);
            rows = Math.Max(rows, r);
            cols = Math.Max(cols, c);
        }

        var rentedElementArgs = ArrayPool<CellValue>.Shared.Rent(nArgs);
        var elementArgs = rentedElementArgs.AsSpan(0, nArgs);
        try
        {
            var result = new CellValue[rows][];
            for (var r = 0; r < rows; r++)
            {
                var resultRow = new CellValue[cols];
                for (var c = 0; c < cols; c++)
                {
                    CellValue? elementError = null;
                    for (var i = 0; i < nArgs; i++)
                    {
                        if (!lifted[i])
                        {
                            elementArgs[i] = args[i];
                            continue;
                        }

                        var type = GetParameterDefinition(paramDefinitions, i).Type;
                        var element = ArrayBroadcaster.ElementAt(args[i], r, c);
                        elementArgs[i] = type == ParameterType.Any
                            ? element
                            : _parameterConverter.ConvertVal(element, type);
                        if (elementArgs[i].IsError() && !acceptsErrors)
                        {
                            elementError ??= elementArgs[i];
                        }
                    }

                    resultRow[c] = elementError ?? func.Invoke(elementArgs, callMetaData);
                }

                result[r] = resultRow;
            }

            return CellValue.Array(result);
        }
        finally
        {
            ArrayPool<CellValue>.Shared.Return(rentedElementArgs, clearArray: true);
        }
    }

    /// <summary>
    /// The definition for the argument at <paramref name="argIndex"/>. Only the last parameter can
    /// repeat, so arguments past the end of the definitions reuse it.
    /// </summary>
    private static ParameterDefinition GetParameterDefinition(ParameterDefinition[] paramDefinitions, int argIndex)
    {
        return paramDefinitions[Math.Min(argIndex, paramDefinitions.Length - 1)];
    }

    private static bool IsLiftableParameter(ParameterDefinition parameter) =>
        parameter.Shape == ParameterShape.Scalar && parameter.Type.IsScalar();

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
