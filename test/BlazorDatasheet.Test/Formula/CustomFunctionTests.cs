using System;
using System.Collections.Generic;
using BlazorDatasheet.Formula.Core.Interpreter.Functions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class CustomFunctionTests
{
    [Test]
    public void Required_Args_After_Optional_Throws_Exception()
    {
        Assert.Throws<InvalidFunctionDefinitionException>(() =>
        {
            var func = new CustomFunctionDefinition(
                new Parameter("number_opt", ParameterType.Number, ParameterRequirement.Optional),
                new Parameter("number_opt", ParameterType.Number, ParameterRequirement.Required));
        });
    }

    [Test]
    public void Repeat_Args_Defined_Before_End_Throws_Exception()
    {
        Assert.Throws<InvalidFunctionDefinitionException>(() =>
        {
            var func = new CustomFunctionDefinition(
                new Parameter("number_optional", ParameterType.NumberSequence, ParameterRequirement.Required, true),
                new Parameter("number_optional", ParameterType.NumberSequence, ParameterRequirement.Optional));
        });
    }

    [Test]
    public void Valid_Param_Definition_Does_Not_Throw_Exception()
    {
        Assert.DoesNotThrow(() =>
        {
            var func = new CustomFunctionDefinition(
                new Parameter("number_required", ParameterType.Number, ParameterRequirement.Required),
                new Parameter("number_optional", ParameterType.Number, ParameterRequirement.Optional),
                new Parameter("number_repeating", ParameterType.NumberSequence, ParameterRequirement.Optional)
            );
        });
    }
}

public class CustomFunctionDefinition : CallableFunctionDefinition
{
    public CustomFunctionDefinition(params Parameter[] parameters) : base(parameters)
    {
    }

    public override object Call(IEnumerable<object> arguments)
    {
        throw new NotImplementedException();
    }

    public override Type ReturnType => typeof(string);
    public override bool AcceptsErrors => false;
}