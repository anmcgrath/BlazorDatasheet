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
        environment.RegisterFunction(ThrowingFunction.Descriptor);
        var formulaEngine = new BlazorDatasheet.Core.FormulaEngine.FormulaEngine(environment);

        Action action = () => formulaEngine.SetVariable("x", "=THROWFN()");

        action.Should().Throw<InvalidOperationException>();
        formulaEngine.IsCalculating.Should().BeFalse();
    }
}

internal static class ThrowingFunction
{
    public static FunctionDescriptor Descriptor { get; } = new(
        "THROWFN",
        [],
        (_, _) => throw new InvalidOperationException("Boom"),
        acceptsErrors: false,
        isVolatile: false);
}
