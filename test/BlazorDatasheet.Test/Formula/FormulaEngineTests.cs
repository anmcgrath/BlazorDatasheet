using System;
using BlazorDatasheet.Formula.Core;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class FormulaEngineTests
{
    [Test]
    public void CalculateSheet_WhenEvaluationThrows_ResetsIsCalculating()
    {
        var environment = new TestEnvironment();
        environment.RegisterFunction("THROWFN", new ThrowingFunction());
        var formulaEngine = new BlazorDatasheet.Core.FormulaEngine.FormulaEngine(environment);

        Action action = () => formulaEngine.SetVariable("x", "=THROWFN()");

        action.Should().Throw<InvalidOperationException>();
        formulaEngine.IsCalculating.Should().BeFalse();
    }
}

internal class ThrowingFunction : ISheetFunction
{
    public ParameterDefinition[] GetParameterDefinitions() => [];
    public CellValue Call(CellValue[] args, FunctionCallMetaData metaData) =>
        throw new InvalidOperationException("Boom");
    public bool AcceptsErrors => false;
    public bool IsVolatile => false;
}
