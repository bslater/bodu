# Formats.Excel Samples

A console application demonstrating the `Bodu.Formats.Excel.Binary` package — the read-only
Excel 97-2003 (BIFF8 / `.xls`) workbook reader. Run it with:

```bash
dotnet run --project samples/Formats.Excel/Bodu.Formats.Excel.Binary.Samples.ExcelReading
```

The sample is offline and deterministic: every scenario reads the committed
`Data/sample-biff8.xls` fixture (464 KB — a genuine two-sheet exchange-rates workbook with
~18,000 cells, copied from the library's test fixtures).

## Sample → pattern → package matrix

| Sample | Demonstrates | Packages |
|---|---|---|
| `Bodu.Formats.Excel.Binary.Samples.ExcelReading` | The `ExcelBinaryWorkbook` session (sheet directory, document properties, date system), the forward-only `ExcelWorksheetReader` streaming ~18k cells in constant memory, the materialized `ExcelWorksheet` convenience surface with coordinate lookup and LINQ, and cell-kind decoding including BIFF8's format-classified serial dates via `ExcelSerialDate`/`ExcelDateSystem` | `Bodu.Formats.Excel.Binary` |
