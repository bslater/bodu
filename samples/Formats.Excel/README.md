# Formats.Excel Samples

A console application demonstrating the `Bodu.Formats.Excel.Binary` package - the read-only
Excel 97-2003 (BIFF8 / `.xls`) workbook reader. Run it with:

```bash
dotnet run --project samples/Formats.Excel/Bodu.Formats.Excel.Binary.Samples.ExcelReading
```

The sample is offline and deterministic: every scenario reads a workbook the sample writes
first - a two-sheet BIFF8 workbook of synthetic daily readings, about 2,400 cells, built with
the codec's `BiffWriter` and the container's `CompoundStorageBuilder` - so it carries no data
file.

## Sample → pattern → package matrix

| Sample | Demonstrates | Packages |
|---|---|---|
| `Bodu.Formats.Excel.Binary.Samples.ExcelReading` | The `ExcelBinaryWorkbook` session (sheet directory, document properties, date system), the forward-only `ExcelWorksheetReader` streaming every cell without a cell map, the materialized `ExcelWorksheet` convenience surface with coordinate lookup and LINQ, and cell-kind decoding including BIFF8's format-classified serial dates via `ExcelSerialDate`/`ExcelDateSystem` | `Bodu.Formats.Excel.Binary` |
