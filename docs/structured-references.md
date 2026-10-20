# Tables and Structured References

A table names a region of a sheet so that formulas can refer to its columns by name, as in Excel:
`=SUM(Sales[Amount])`, `=[@Qty]*[@Price]`, `=Sales[[#Headers],[Amount]]`.

Tables only affect formulas and the formula editor (autocomplete and reference highlighting). They have no
visual styling. A demo is on the `/Tables` page of the demo apps (`TableExample.razor`).

## Creating and changing tables

Tables are workbook-wide and live in `Workbook.Tables` (`TableCollection`).

```csharp
var workbook = new Workbook();
var sheet = workbook.AddSheet(100, 10);

// A region table over A1:D6. Row 1 is the header row; rows 2-6 are data.
SheetTable? sales = workbook.Tables.Add("Sales", sheet.Name, new Region(0, 5, 0, 3));

// A sheet table: every row of the sheet is data, the column headings are the column names.
workbook.Tables.AddSheetTable("Data", otherSheet.Name);

workbook.Tables.Rename("Sales", "Orders");                 // rewrites Sales[...] in formulas
workbook.Tables.Resize("Orders", new Region(0, 9, 0, 3));   // region tables only, same sheet
workbook.Tables.SetShowTotals("Orders", true);             // the last row becomes the totals row
workbook.Tables.Remove("Orders");                          // references become #REF!, text kept
```

- `Add`/`AddSheetTable` return `null`, and `Remove`/`Rename`/`Resize`/`SetShowTotals` return `false`, when the
  operation is not valid. `IsValidTableName(name, out reason)` explains why a name can't be used.
- All of these are commands on the table's sheet and can be undone and redone.
- A region table must be a bounded `Region` (not a `ColumnRegion` or `RowRegion` - use a sheet table for whole
  columns), fit in its sheet, have a header row and at least one data row (plus the totals row when it is shown,
  so at least three rows with totals) and not overlap another table. `SetShowTotals(name, true)` returns false
  for a two-row table, and `Resize` rejects a region that would leave no data row.
  A sheet has either one sheet table or any number of region tables, not both.
- Queries: `Get(name)`, `Contains(name)`, `GetAll()`, `GetOnSheet(sheetName)`, `GetTableAt(sheetName, row, col)`.
  `SheetTable` is a snapshot with `Name`, `SheetName`, `Region`, `HeaderRegion`, `DataRegion`, `TotalsRegion`,
  `ColumnNames`, `IsSheetTable`, `ShowTotals` and `IsDeleted`.
- `TableChanged` is raised (including on undo/redo) with a `TableChangeKind` of `Added`, `Removed`, `Renamed`
  (with `OldName`), `Resized`, `ShowTotalsChanged` or `ColumnsRenamed` (with `ColumnRenames`).
- Tables are serialized with the workbook.

## Syntax

| Reference | Refers to |
|---|---|
| `Sales[Amount]` | The data cells of the Amount column |
| `Sales[]` | All data cells |
| `Sales[@Amount]`, `Sales[[#This Row],[Amount]]` | The Amount cell in the formula's row (`#VALUE!` outside the data rows) |
| `Sales[@]` | The whole table row of the formula |
| `[@Amount]`, `[Amount]`, `[#Headers]`... | Unqualified: the table containing the formula cell |
| `Sales[#All]` | Header, data and totals rows |
| `Sales[#Data]` | Data rows |
| `Sales[#Headers]` | Header row |
| `Sales[#Totals]` | Totals row (`#REF!` if not shown) |
| `Sales[[#Headers],[Amount]]` | One part of one column |
| `Sales[[Qty]:[Amount]]`, `Sales[[#Data],[Qty]:[Amount]]` | A range of columns |
| `Sales[[#Headers],[#Data]]`, `Sales[[#Data],[#Totals]]` | The only allowed specifier pairs |

- At most one specifier, or one of the two pairs above. Specifiers come before columns. At most one column or
  one `:` column range. `@` cannot be combined with other specifiers.
- `;` may be used instead of `,` between items.
- Specifiers, table names and column names are case-insensitive. Formulas are rewritten in canonical form
  (`#This Row` is written as `@`).
- `[ ] # ' @` in a column name are escaped with `'`, e.g. `Sales['#Sold]` for a column named `#Sold`.
- There must be no space between the table name and `[`: `Sales [Amount]` is not a structured reference.
- Unknown tables or columns evaluate to `#REF!`; the formula text is kept, and it works again once the table or
  column exists.
- Structured references can be used in named ranges/variables (`NamedRanges.Set("amounts", "Sales[Amount]")`) and
  conditional format formulas (`=[@Amount]>10`). `@` in a variable is `#VALUE!` because it has no row.

## Column names

- Region tables: the displayed text of the header row. A number or date uses the cell's number format if it has
  one; otherwise a number is written in the invariant culture (`1.5`) and a date as `yyyy-MM-dd` (with
  ` HH:mm:ss` if it has a time). Booleans are `TRUE`/`FALSE`. Editing a header cell renames the column and
  rewrites formulas that refer to it (undoing the edit reverts both). Changing a header cell's number format with
  `SetFormat`/`ClearFormat` (or undoing it) also renames the column if its text changes; other format changes
  (e.g. pasting formats) take effect the next time the header cell's value changes.
- As in Excel, whitespace just inside an item's brackets is ignored: `Sales[ Amount ]`, `Sales[[ #Data ],[ Amount ]]`
  and `Sales[@ Amount ]` refer to `Amount` and are rewritten without the padding. Leading and trailing whitespace
  in a header is also ignored when matching, so `Sales[Amount]` finds a header ` Amount `. Whitespace inside a
  name (`Sales[Unit Price]`) is significant. Changing only the whitespace around a header is not a rename.
- Empty or whitespace-only names become `Column{n}`, `n` being the one-based column position in the table.
- Duplicates (ignoring case and surrounding whitespace): the first keeps the name, later ones are trimmed and get
  the smallest free numeric suffix from 2, e.g. `Qty`, ` Qty ` become `Qty`, `Qty2`. Cells are never changed.

## Sheet tables

- Column names are the sheet's column headings (`sheet.Columns.SetHeadings`); changing a heading renames the
  column and rewrites formulas that refer to it, like a header cell edit.
- Every row is data, and grows with the sheet. `[@Col]`/`[Col]` work anywhere on the sheet.
- `Data[#Headers]` returns the heading strings as an array, since there are no header cells.
- There is no totals row: `#Totals` is `#REF!` and `SetShowTotals`/`Resize` return false.

## Rows and columns inserted or removed

- Region tables shift and grow like named ranges: inserting rows/columns inside a table grows it; inserting
  directly below or to the right of it does not.
- Removing rows/columns inside a table shrinks it. Removing all of it marks the table deleted and references
  become `#REF!` (text preserved); undo restores it.
- When a table's shape changes (e.g. its header row is removed, so the first remaining row becomes the header, or
  columns are inserted/removed), column names are re-read silently: formulas are not rewritten, and references to
  names that no longer exist become `#REF!`.

## Names

- Table names follow the variable name rules (a letter or `_`, then letters, digits, `_ . ? \`), must not look
  like a cell/row/column address or a boolean, and share a namespace with variables, named ranges and functions,
  ignoring case. `SetVariable` and `NamedRanges.Set` reject names used by tables.

## Limitations

- No visual table styling, banding, filter buttons or automatic totals formulas.
- `Workbook.DuplicateSheet` does not copy a sheet's tables.
- Unqualified references (`[@Col]`) only resolve inside a region table (or anywhere on a sheet with a sheet table);
  elsewhere they are `#REF!`.
- Tables on a removed sheet are detached and come back when a sheet of that name is added again.
