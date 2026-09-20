using System;
using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Formula.Core;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class FunctionRegistryTests
{
    [Test]
    public void TryGetFunction_IsCaseInsensitive()
    {
        var descriptor = new FunctionDescriptor(
            "SUMX",
            [],
            (_, _) => CellValue.Number(1));

        var registry = new FunctionRegistryBuilder()
            .Add(descriptor)
            .Build();

        var found = registry.TryGetFunction("sumx", out var result);

        Assert.That(found, Is.True);
        Assert.That(result.Name, Is.EqualTo("SUMX"));
    }

    [Test]
    public void Builder_Rejects_Duplicate_Function_Names()
    {
        var builder = new FunctionRegistryBuilder();
        builder.Add(new FunctionDescriptor("DUP", [], (_, _) => CellValue.Empty));

        Assert.Throws<InvalidOperationException>(() =>
            builder.Add(new FunctionDescriptor("dup", [], (_, _) => CellValue.Empty)));
    }

    [Test]
    public void Builder_IsImmutable_AfterBuild()
    {
        var builder = new FunctionRegistryBuilder();
        builder.Add(new FunctionDescriptor("A", [], (_, _) => CellValue.Empty));
        _ = builder.Build();

        Assert.Throws<InvalidOperationException>(() =>
            builder.Add(new FunctionDescriptor("B", [], (_, _) => CellValue.Empty)));
    }

    [Test]
    public void FunctionDescriptor_Computes_Arity_Cache()
    {
        var descriptor = new FunctionDescriptor(
            "TEST",
            [
                new ParameterDefinition("req", ParameterType.Number, ParameterRequirement.Required),
                new ParameterDefinition("opt", ParameterType.Number, ParameterRequirement.Optional),
                new ParameterDefinition("repeat", ParameterType.Number, ParameterRequirement.Optional, isRepeating: true)
            ],
            (_, _) => CellValue.Empty);

        Assert.That(descriptor.MinArity, Is.EqualTo(1));
        Assert.That(descriptor.MaxArity, Is.EqualTo(128));
    }

    [Test]
    public void Descriptions_Are_Optional_And_Kept()
    {
        var plain = new FunctionDescriptor("PLAIN", [new ParameterDefinition("x", ParameterType.Number)],
            (_, _) => CellValue.Number(1));
        var described = new FunctionDescriptor(
            "DESCRIBED",
            [new ParameterDefinition("x", ParameterType.Number, description: "A number.")],
            (_, _) => CellValue.Number(1),
            description: "Does something.");

        Assert.That(plain.Description, Is.Null);
        Assert.That(plain.ParameterDefinitions[0].Description, Is.Null);
        Assert.That(described.Description, Is.EqualTo("Does something."));
        Assert.That(described.ParameterDefinitions[0].Description, Is.EqualTo("A number."));
    }

    [Test]
    public void Built_In_Functions_And_Their_Parameters_Are_Described()
    {
        var functions = Workbook.BuildDefaultRegistry(options: null).SearchForFunctions("").ToList();

        Assert.That(functions, Is.Not.Empty);
        foreach (var function in functions)
        {
            Assert.That(function.Function.Description, Is.Not.Null.And.Not.Empty, function.Name);
            foreach (var parameter in function.Function.ParameterDefinitions)
                Assert.That(parameter.Description, Is.Not.Null.And.Not.Empty, $"{function.Name} {parameter.Name}");
        }
    }
}
