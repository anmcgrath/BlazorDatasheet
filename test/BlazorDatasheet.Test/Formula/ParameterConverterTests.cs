using System.Collections.Generic;
using BlazorDatasheet.DataStructures.Cells;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Functions;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class ParameterConverterTests
{
    private ParameterToArgConverter _toArgConverter;

    [SetUp]
    public void Setup()
    {
        _toArgConverter = new(new TestEnvironment());
    }

    [Test]
    public void Scalar_Param_Converts_To_Simple_Number()
    {
        var paramDef = new ParameterDefinition(
            "test",
            ParameterType.Number,
            ParameterDimensionality.Scalar,
            ParameterRequirement.Required,
            false);

        _toArgConverter.ToArg(2, paramDef).Value.Should().BeEquivalentTo(new CellValue(2));
    }

    [Test]
    public void Repeating_Scalar_Param_Converts_To_Array()
    {
        var paramDef = new ParameterDefinition(
            "test",
            ParameterType.Number,
            ParameterDimensionality.Scalar,
            ParameterRequirement.Required,
            true);

        _toArgConverter.ToArg(2, paramDef).Value.Should().BeEquivalentTo(new[] { new CellValue(2) });
    }

    [Test]
    public void Range_Param_Converts_Single_Number_To_Range()
    {
        var paramDef = new ParameterDefinition(
            "test",
            ParameterType.Number,
            ParameterDimensionality.Range,
            ParameterRequirement.Required,
            false);

        var arg = _toArgConverter.ToArg(2, paramDef);
        arg.Value.Should().BeEquivalentTo(new[] { new[] { new CellValue(2) } });
    }
}