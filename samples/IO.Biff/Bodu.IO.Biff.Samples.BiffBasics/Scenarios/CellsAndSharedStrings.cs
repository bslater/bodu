// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CellsAndSharedStrings.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff.Samples.BiffBasics.Scenarios;

/// <summary>
/// Demonstrates the typed accessors: the sheet directory from <c>BOUNDSHEET</c>, the shared string table read one
/// string at a time through <see cref="BiffSstReader" /> across its <c>CONTINUE</c> records, and the cell records
/// of the first sheet decoded into values — the raw material the Excel reader turns into <c>ExcelCell</c>s.
/// </summary>
public static class CellsAndSharedStrings
{
    /// <summary>
    /// Reads the globals for the sheet directory and shared strings, then decodes the first sheet's cells.
    /// </summary>
    /// <param name="stream">The workbook stream bytes.</param>
    public static void Run(byte[] stream)
    {
        Console.WriteLine("--- Cells and shared strings ---");

        var reader = new BiffReader(stream);
        var sheets = new List<(string Name, uint Offset)>();
        string[] sharedStrings = [];

        // The globals substream ends at the first EOF; everything the sheets depend on is in it.
        while (reader.Read() && reader.RecordType != BiffRecordType.Eof)
        {
            switch (reader.RecordType)
            {
                case BiffRecordType.BoundSheet:
                    BiffBoundSheetRecord sheet = reader.GetBoundSheet();
                    sheets.Add((sheet.Name.GetString(), sheet.StreamOffset));
                    break;

                case BiffRecordType.Sst:
                    sharedStrings = ReadSharedStrings(ref reader);
                    break;
            }
        }

        Console.WriteLine($"  {sheets.Count} sheets; {sharedStrings.Length} shared strings");
        foreach ((string name, uint offset) in sheets)
            Console.WriteLine($"    '{name}' at offset {offset}");

        // A sheet substream starts with its own BOF, but seeding the version and code page from the globals lets the
        // version-dependent accessors work from the first record regardless.
        var sheetReader = new BiffReader(stream.AsSpan((int)sheets[0].Offset), new BiffReaderOptions { Version = reader.Version, CodePage = reader.CodePage });
        int shown = 0;
        int cells = 0;

        while (sheetReader.Read() && sheetReader.RecordType != BiffRecordType.Eof)
        {
            string? description = sheetReader.RecordType switch
            {
                BiffRecordType.Number => Describe(sheetReader.GetNumber()),
                BiffRecordType.Rk => Describe(sheetReader.GetRk()),
                BiffRecordType.LabelSst => Describe(sheetReader.GetLabelSst(), sharedStrings),
                BiffRecordType.Label => Describe(sheetReader.GetLabel()),
                BiffRecordType.BoolErr => Describe(sheetReader.GetBoolErr()),
                BiffRecordType.Formula => Describe(sheetReader.GetFormula()),
                BiffRecordType.MulRk => Describe(sheetReader.GetMulRk()),
                _ => null,
            };

            if (description is null)
                continue;

            cells++;
            if (shown++ < 12)
                Console.WriteLine($"    {description}");
        }

        Console.WriteLine($"  {cells} value-bearing cell records in '{sheets[0].Name}' (first 12 shown)");
        Console.WriteLine();
    }

    /// <summary>
    /// Materializes the shared string table the reader is positioned on.
    /// </summary>
    /// <param name="reader">The reader, positioned on the SST record.</param>
    /// <returns>The unique strings.</returns>
    private static string[] ReadSharedStrings(ref BiffReader reader)
    {
        var strings = new BiffSstReader(ref reader);
        var result = new List<string>((int)Math.Min(strings.Header.UniqueCount, 4096));
        int fragmented = 0;

        while (strings.Read(ref reader))
        {
            // A string that lies within one record has a span-backed view; one that straddled a CONTINUE boundary
            // was stitched into a scratch buffer and is served by GetString().
            if (strings.IsFragmented)
                fragmented++;

            result.Add(strings.GetString());
        }

        Console.WriteLine($"  SST: {strings.Header.UniqueCount} unique / {strings.Header.TotalCount} references; {fragmented} straddled a CONTINUE boundary");
        return [.. result];
    }

    private static string Describe(BiffNumberRecord r) =>
        $"R{r.Row}C{r.Column} NUMBER   {r.Value}";

    private static string Describe(BiffRkRecord r) =>
        $"R{r.Row}C{r.Column} RK       {r.Value} (raw 0x{r.RawValue:X8})";

    private static string Describe(BiffLabelSstRecord r, string[] sharedStrings) =>
        $"R{r.Row}C{r.Column} LABELSST \"{sharedStrings[r.SstIndex]}\" (index {r.SstIndex})";

    private static string Describe(in BiffLabelRecord r) =>
        $"R{r.Row}C{r.Column} LABEL    \"{r.Text.GetString()}\"";

    private static string Describe(BiffBoolErrRecord r) =>
        r.IsError ? $"R{r.Row}C{r.Column} BOOLERR  error 0x{r.ErrorCode:X2}" : $"R{r.Row}C{r.Column} BOOLERR  {r.BooleanValue}";

    private static string Describe(in BiffFormulaRecord r) =>
        $"R{r.Row}C{r.Column} FORMULA  cached {r.CachedResultKind}" + (r.CachedResultKind == BiffCachedResultKind.Number ? $" {r.NumberValue}" : string.Empty) + $", {r.Tokens.Length} token bytes";

    private static string Describe(in BiffMulRkRecord r)
    {
        var values = new double[r.Count];
        for (int i = 0; i < values.Length; i++)
            values[i] = r[i].Value;

        return $"R{r.Row}C{r.FirstColumn}..C{r.LastColumn} MULRK {r.Count} cells: {string.Join(", ", values)}";
    }
}
