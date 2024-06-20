using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Core.Validation;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Validation;

public class ValidationManagerTests
{
    private Sheet _sheet;
    private ValidationManager _validationManager;

    [SetUp]
    public void Setup()
    {
        _sheet = new Sheet(5, 5);
        _validationManager = _sheet.Validators;
    }


    [Test]
    public void Add_Validator_Returns_Validator_In_Region()
    {
        _validationManager.Add(new Region(0, 5, 0, 5), new NumberValidator(true));
        _validationManager.Get(0, 0).Should().NotBeEmpty();
        _validationManager.Get(1, 1).Should().NotBeEmpty();
        _validationManager.Get(6, 0).Should().BeEmpty();
    }

    [Test]
    public void Validation_Validates_On_Cell_With_Validators()
    {
        var validator = new AlwaysFalseValidator(true);
        _validationManager.Add(new Region(0, 2, 0, 2), validator);
        var validationInside = _validationManager.Validate(CellValue.Number(0), 0, 0);
        validationInside.IsValid.Should().BeFalse();
        validationInside.IsStrictFail.Should().Be(validator.IsStrict);
        validationInside.FailMessages.Should().ContainSingle(validator.Message);

        var validationOutside = _validationManager.Validate(CellValue.Number(0), 3, 0);
        validationOutside.IsValid.Should().BeTrue();
        validationOutside.IsStrictFail.Should().BeFalse();
        validationOutside.FailMessages.Should().BeEmpty();
    }

    [Test]
    public void Remove_Validator_From_Region_Results_In_No_Validation()
    {
        var falseValidator = new AlwaysFalseValidator(true);
        _validationManager.Add(new Region(0, 5, 0, 5), falseValidator);
        _validationManager.Clear(falseValidator, new Region(2, 3, 2, 3));
        _validationManager.Validate(CellValue.Number(-1), 0, 0).IsValid.Should().BeFalse();
        _validationManager.Validate(CellValue.Number(-1), 2, 2).IsValid.Should().BeTrue();
    }

    [Test]
    public void Insert_Row_At_Top_Of_Validator_Shifts_It_Down()
    {
        var sheet = new Sheet(4, 4);
        var val = new SourceValidator(new List<string>() { "A", "B" }, false);
        sheet.Validators.Add(2, 2, val);
        sheet.Rows.InsertAt(2);
        sheet.Validators.Get(1, 2).Should().BeEmpty();
        sheet.Validators.Get(2, 2).Should().BeEmpty();
        sheet.Validators.Get(3, 2).First().Should().BeSameAs(val);

        sheet.Commands.Undo();
        sheet.Validators.Get(3, 2).Should().BeEmpty();
        sheet.Validators.Get(2, 2).First().Should().BeSameAs(val);
    }

    [Test]
    public void Insert_Col_At_Left_Of_Validator_Shifts_It_Right()
    {
        var sheet = new Sheet(4, 4);
        var val = new SourceValidator(new List<string>() { "A", "B" }, false);
        sheet.Validators.Add(2, 2, val);
        sheet.Columns.InsertAt(2);
        sheet.Validators.Get(2, 1).Should().BeEmpty();
        sheet.Validators.Get(2, 2).Should().BeEmpty();
        sheet.Validators.Get(2, 3).First().Should().BeSameAs(val);

        sheet.Commands.Undo();
        sheet.Validators.Get(2, 3).Should().BeEmpty();
        sheet.Validators.Get(2, 2).First().Should().BeSameAs(val);
    }

    [Test]
    public void Insert_Col_At_Left_Of_Validator_WhenAtCol0_Shifts_It_Right()
    {
        var sheet = new Sheet(4, 4);
        var val = new SourceValidator(new List<string>() { "A", "B" }, false);
        sheet.Validators.Add(2, 0, val);
        sheet.Columns.InsertAt(0);
        sheet.Validators.Get(2, 0).Should().BeEmpty();
        sheet.Validators.Get(2, 1).First().Should().BeSameAs(val);

        sheet.Commands.Undo();
        sheet.Validators.Get(2, 1).Should().BeEmpty();
        sheet.Validators.Get(2, 0).First().Should().BeSameAs(val);
    }

    internal class AlwaysFalseValidator : IDataValidator
    {
        public AlwaysFalseValidator(bool isStrict)
        {
            IsStrict = isStrict;
        }

        public bool IsValid(CellValue value)
        {
            return false;
        }

        public bool IsStrict { get; }
        public string Message => "Always invalid";
    }

    internal class AlwaysTrueValidator : IDataValidator
    {
        public AlwaysTrueValidator(bool isStrict)
        {
            IsStrict = isStrict;
        }

        public bool IsValid(CellValue value)
        {
            return false;
        }

        public bool IsStrict { get; }
        public string Message => "Always valid";
    }
}