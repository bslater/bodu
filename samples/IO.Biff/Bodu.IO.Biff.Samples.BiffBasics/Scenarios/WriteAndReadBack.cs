// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WriteAndReadBack.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Buffers.Binary;

namespace Bodu.IO.Biff.Samples.BiffBasics.Scenarios;

/// <summary>
/// Demonstrates <see cref="BiffWriter" />: a minimal BIFF8 workbook stream — globals with a shared string table and
/// one bound sheet, then the sheet with every cell kind — assembled with <see cref="BiffWriter.BytesCommitted" />
/// supplying the sheet offset, and read straight back through <see cref="BiffReader" />. The same code under
/// <see cref="BiffVersion.Biff5" /> writes code-page text and the BIFF5 layouts instead.
/// </summary>
public static class WriteAndReadBack
{
    /// <summary>
    /// Writes a two-substream workbook stream and reads it back.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Write a workbook stream and read it back ---");

        byte[] stream = Write();
        Console.WriteLine($"  wrote {stream.Length} bytes");

        var reader = new BiffReader(stream);
        string[] sharedStrings = [];
        while (reader.Read())
        {
            switch (reader.RecordType)
            {
                case BiffRecordType.Bof:
                    Console.WriteLine($"  {reader.Version} {reader.GetBof().SubstreamType} substream at {reader.RecordStartIndex}");
                    break;
                case BiffRecordType.BoundSheet:
                    BiffBoundSheetRecord sheet = reader.GetBoundSheet();
                    Console.WriteLine($"    sheet '{sheet.Name.GetString()}' -> offset {sheet.StreamOffset}");
                    break;
                case BiffRecordType.Sst:
                    var strings = new BiffSstReader(ref reader);
                    var list = new List<string>();
                    while (strings.Read(ref reader))
                        list.Add(strings.GetString());
                    sharedStrings = [.. list];
                    Console.WriteLine($"    SST: {string.Join(", ", sharedStrings.Select(s => $"\"{s}\""))}");
                    break;
                case BiffRecordType.LabelSst:
                    BiffLabelSstRecord label = reader.GetLabelSst();
                    Console.WriteLine($"    R{label.Row}C{label.Column} = \"{sharedStrings[label.SstIndex]}\"");
                    break;
                case BiffRecordType.Number:
                    BiffNumberRecord number = reader.GetNumber();
                    Console.WriteLine($"    R{number.Row}C{number.Column} = {number.Value}");
                    break;
                case BiffRecordType.Rk:
                    BiffRkRecord rk = reader.GetRk();
                    Console.WriteLine($"    R{rk.Row}C{rk.Column} = {rk.Value} (RK 0x{rk.RawValue:X8})");
                    break;
                case BiffRecordType.BoolErr:
                    BiffBoolErrRecord flag = reader.GetBoolErr();
                    Console.WriteLine($"    R{flag.Row}C{flag.Column} = {(flag.IsError ? $"error 0x{flag.ErrorCode:X2}" : flag.BooleanValue.ToString())}");
                    break;
                case BiffRecordType.Formula:
                    BiffFormulaRecord formula = reader.GetFormula();
                    Console.WriteLine($"    R{formula.Row}C{formula.Column} = formula, cached {formula.CachedResultKind}");
                    break;
                case BiffRecordType.String:
                    Console.WriteLine($"      cached text \"{reader.GetString().Text.GetString()}\"");
                    break;
            }
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Writes the globals and one sheet, patching the bound-sheet offset once the sheet position is known.
    /// </summary>
    /// <returns>The workbook stream bytes.</returns>
    private static byte[] Write()
    {
        var output = new ArrayBufferWriter<byte>();
        var writer = new BiffWriter(output, BiffVersion.Biff8);

        writer.WriteBof(BiffSubstreamType.WorkbookGlobals);
        writer.WriteCodePage(1200);
        writer.WriteDateMode(false);
        writer.WriteFont(200, 0, 0x7FFF, 400, 0, 0, 0, 0, "Arial");
        writer.WriteXf(new BiffXfRecord(0, 0, 0xFFF5));
        writer.WriteSst(["Product", "Widget", "Gadget"], totalReferenceCount: 3);

        // The bound sheet's offset is the sheet's BOF position, which is not known yet; record where the field is.
        long boundSheetAt = writer.BytesCommitted;
        writer.WriteBoundSheet(0, BiffSheetState.Visible, BiffSheetType.Worksheet, "Sales");
        writer.WriteEof();

        long sheetAt = writer.BytesCommitted;
        writer.WriteBof(BiffSubstreamType.Worksheet);
        writer.WriteDimensions(new BiffDimensionsRecord(0, 3, 0, 3));
        writer.WriteLabelSst(0, 0, 0, 0);
        writer.WriteLabelSst(1, 0, 0, 1);
        writer.WriteNumber(1, 1, 0, 1234.5);
        if (BiffRk.TryEncode(0.25, out uint rk))
            writer.WriteRk(1, 2, 0, rk);
        writer.WriteLabelSst(2, 0, 0, 2);
        writer.WriteBoolean(2, 1, 0, true);
        writer.WriteFormula(2, 2, 0, BiffCachedResultKind.String, 0, tokens: default);
        writer.WriteString("cached result");
        writer.WriteEof();

        // Patch the offset in place: the BOUNDSHEET payload begins four bytes after its header.
        byte[] stream = output.WrittenSpan.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(stream.AsSpan((int)boundSheetAt + BiffLimits.RecordHeaderSize), (uint)sheetAt);
        return stream;
    }
}
