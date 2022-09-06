# BlazorDatasheet

A simple datasheet component for editing tabular data.

#### Features
- Data editing
  - Built in editors including text, date, select, boolean
  - Add custom editors for any data type
- Conditional formatting
- Data validation
- Build datasheet from an object definition
- Keyboard navigation
- Virtualization via Blazor Virtualization - handles many cells at once.

Demo: https://anmcgrath.github.io/BlazorDatasheet/

### Getting Started

Blazor Datasheet provides a **Datasheet** Blazor component that accepts a Sheet.

A Sheet holds the data and configuration for a Datasheet. The data is set per Cell, or can be built using the ObjectEditorBuilder, which creates a datasheet based on a list of objects.

The following code displays a 3 x 3 data sheet of empty strings.

```csharp
<Datasheet
    Sheet="sheet"/>

@code{

    private Cell[,] cells = new Cell[3, 3]
    {
        { new Cell(""), new Cell(""), new Cell("") },
        { new Cell(""), new Cell(""), new Cell("") },
        { new Cell(""), new Cell(""), new Cell("") },
    };

    private Sheet sheet;

    protected override void OnInitialized()
    {
        sheet = new Sheet(numRows: 3, numCols: 3, cells: cells);
    }

}
```

The default editor is the text editor, but can be changed by defining the Type property of each cell.