# BlazorDatasheet

A simple datasheet component for editing tabular data.

![image](https://user-images.githubusercontent.com/34253568/197425287-690a747a-24f5-4e0d-afcf-e2a09efbaba2.png)

#### Features
- Data editing
  - Built in editors including text, date, select, boolean, text area, enum
  - Add custom editors for any data type
- Conditional formatting
- Data validation
- Build datasheet from an object definition
- Keyboard navigation
- Copy and paste from tabulated data
- Virtualization via Blazor Virtualization - handles many cells at once.

Demo: https://anmcgrath.github.io/BlazorDatasheet/

### Getting Started


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
