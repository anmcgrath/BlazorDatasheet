using System;
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
}
