# BlazorDatasheet

A simple datasheet component for editing tabular data.

<img width="512" alt="DatasheetScreenshot" src="https://github.com/anmcgrath/BlazorDatasheet/assets/34253568/afb4fd86-f082-4cdf-bac9-5d4f777c7dcc">

#### Features
- Data editing
  - Built in editors including text, date, select, boolean, text area, enum
  - Add custom editors for any data type
- Conditional formatting
- Data validation
- Formula
- Keyboard navigation
- Copy and paste from tabulated data
- Virtualization - handles many cells at once in both rows & cols.

Demo: https://anmcgrath.github.io/BlazorDatasheet/

### Getting Started

#### Install the NuGet package

```bash
dotnet add package BlazorDatasheet
```

#### Configure Program.cs

In Program.cs, add the required services:

```csharp
builder.Services.AddBlazorDatasheet();
```

#### Import JS/CSS

In _Layout.cshtml or index.html add

```htmlinblazor
<link href="_content/BlazorDatasheet/sheet-styles.css" rel="stylesheet"/>
```

and

```htmlinblazor
<script src="_content/BlazorDatasheet/blazor-datasheet.js" type="text/javascript"></script>
```

Blazor Datasheet provides a **Datasheet** Blazor component that accepts a Sheet.

A Sheet holds the data and configuration for a Datasheet. The data is set per Cell, or can be built using the ObjectEditorBuilder, which creates a datasheet based on a list of objects.

The following code displays an empty 3 x 3 data grid.

```csharp
<Datasheet
    Sheet="sheet"/>

@code{

    private Sheet sheet;

    protected override void OnInitialized()
    {
        sheet = new Sheet(3, 3);
    }

}
```

The default editor is the text editor, but can be changed by defining the Type property of each cell.

### Setting & getting cell values

Cell values can be set in a few ways:

```csharp
sheet.Cells[0, 0].Value = "Test"
sheet.Range("A1").Value = "Test";
sheet.Cells.SetValue(0, 0, "Test");
sheet.Commands.ExecuteCommand(new SetCellValueCommand(0, 0, "Test"));
```

In this example, the first two methods set the value but cannot be undone. The last two methods can be undone.

### Formula

Formula can be applied to cells. When the cells or ranges that the formula cells reference change, the cell value is re-calculated.

Currently, the whole sheet is calculated if any referenced cell or range changes.

```csharp
sheet.Cells[0, 0].Formula = "=10+A2"
```

### Formatting

Cell formats can be set in the following ways:

```csharp
sheet.Range("A1:A2").Format = new CellFormat() { BackgroundColor = "red" };
sheet.Commands.ExecuteCommand(
    new SetFormatCommand(new RowRegion(10, 12), new CellFormat() { ForegroundColor = "blue" }));
sheet.SetFormat(sheet.Range(new ColumnRegion(5)), new CellFormat() { FontWeight = "bold" });
sheet.Cells[0, 0].Format = new CellFormat() { TextAlign = "center" };
```

When a cell format is set, it will be merged into any existing cell formats in the region that it is applied to. Any non-null format paremeters will be merged:

```csharp
sheet.Range("A1").Format = new CellFormat() { BackgroundColor = "red" };
sheet.Range("A1:A2").Format = new CellFormat() { ForegroundColor = "blue" };
var format = sheet.Cells[0, 0].Format; // backroundColor = "red", foreground = "blue"
var format2 = sheet.Cells[1, 0].Format; // foreground = "blue"
```

### Cell types
The cell type specifies which renderer and editor are used for the cell.

```csharp
sheet.Range("A1:B5").Type = "boolean"; // renders a checkbox
```

Custom editors and renderers can be defined. See the examples for more information.

### Validation
Data validation can be set on cells/ranges. There are two modes of validation: strict and non-strict. When a validator is strict, the cell value will not be set by the editor if it fails validation.

If validation is not strict, the value can be set during editing but will show a validation error when rendered.

Although a strict validation may be set on a cell, the value can be changed programmatically, but it will display as a validation error.

```csharp
sheet.Validators.Add(new ColumnRegion(0), new NumberValidator(isStrict: true));
```

### Regions and ranges

A region is a geometric construct, for example:

```csharp
var region = new Region(0, 5, 0, 5); // r0 to r5, c0 to c5
var cellRegion = new Region(0, 0); // cell A1
var colRegion = new ColumnRegion(0, 4); // col region spanning A to D
var rowRegion = new RowRegion(0, 3); // row region spanning 1 to 4
```

A range is a of region that also knows about the sheet. Ranges can be used to set certain parts of the sheet.

```csharp
var range = sheet.Range("A1:C5);
var range = sheet.Range(new ColumnRegion(0));
var range = sheet.Range(0, 0, 4, 5);
```
