// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff5TestWorkbook.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.IO.Biff;

namespace Bodu.Formats.Excel;

/// <summary>
/// Builds synthetic BIFF5 (Excel 5.0/95) workbook streams and <c>.xls</c> compound files for tests, emitting every
/// record through a <see cref="BiffWriter" /> configured for <see cref="BiffVersion.Biff5" /> and the given code
/// page, and storing the workbook under the <c>Book</c> stream name those versions used.
/// </summary>
internal static class Biff5TestWorkbook
{
    /// <summary>Writes records through a writer passed by reference.</summary>
    /// <param name="writer">The writer.</param>
    public delegate void WriteAction(ref BiffWriter writer);

    /// <summary>
    /// Builds a complete BIFF5 workbook stream: globals (CODEPAGE, the extra records, one BOUNDSHEET per sheet) and
    /// one worksheet substream per sheet.
    /// </summary>
    /// <param name="codePage">The code page to declare and encode text in.</param>
    /// <param name="globalsExtra">A callback writing additional globals records after CODEPAGE.</param>
    /// <param name="sheets">The sheet names paired with callbacks writing each sheet's body records.</param>
    /// <returns>The workbook stream bytes.</returns>
    public static byte[] BuildWorkbookStream(int codePage, WriteAction? globalsExtra, params (string Name, WriteAction Body)[] sheets)
    {
        var options = new BiffWriterOptions { Version = BiffVersion.Biff5, CodePage = codePage };

        // Each sheet substream is written up front so its length fixes the bound-sheet offsets.
        byte[][] substreams = new byte[sheets.Length][];
        for (int i = 0; i < sheets.Length; i++)
        {
            var sheetOutput = new ArrayBufferWriter<byte>();
            var sheetWriter = new BiffWriter(sheetOutput, options);
            sheetWriter.WriteBof(BiffSubstreamType.Worksheet);
            sheets[i].Body(ref sheetWriter);
            sheetWriter.WriteEof();
            substreams[i] = sheetOutput.WrittenSpan.ToArray();
        }

        // Two passes over the globals: the first measures, the second writes the real offsets.
        long globalsLength = WriteGlobals(options, codePage, globalsExtra, sheets, substreams, 0).Length;
        byte[] globals = WriteGlobals(options, codePage, globalsExtra, sheets, substreams, globalsLength);

        return [.. globals, .. substreams.SelectMany(s => s)];
    }

    /// <summary>
    /// Builds a BIFF5 workbook and wraps it in a compound file under the <c>Book</c> stream name.
    /// </summary>
    /// <param name="codePage">The code page to declare and encode text in.</param>
    /// <param name="globalsExtra">A callback writing additional globals records after CODEPAGE.</param>
    /// <param name="sheets">The sheet names paired with callbacks writing each sheet's body records.</param>
    /// <returns>The compound file bytes, positioned at the start.</returns>
    public static MemoryStream BuildWorkbook(int codePage, WriteAction? globalsExtra, params (string Name, WriteAction Body)[] sheets) =>
        Biff8TestWorkbook.WrapInCompoundFile(BuildWorkbookStream(codePage, globalsExtra, sheets), "Book");

    /// <summary>
    /// Builds a BIFF5 <c>RSTRING</c> cell: a rich-text label with a run table, in the layout Excel 5.0 wrote.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <param name="column">The column.</param>
    /// <param name="text">The text.</param>
    /// <param name="codePage">The code page to encode the text in.</param>
    /// <param name="runCount">The number of formatting runs to append.</param>
    /// <returns>The framed record.</returns>
    public static byte[] RString(int row, int column, string text, int codePage, int runCount)
    {
        byte[] bytes = Encoding.GetEncoding(codePage).GetBytes(text);
        byte[] payload = new byte[6 + 2 + bytes.Length + 1 + (runCount * 2)];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(payload, (ushort)row);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(2), (ushort)column);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(6), (ushort)bytes.Length);
        bytes.CopyTo(payload.AsSpan(8));
        payload[8 + bytes.Length] = (byte)runCount;

        var output = new ArrayBufferWriter<byte>();
        var writer = new BiffWriter(output, BiffVersion.Biff5);
        writer.WriteRecord(BiffRecordType.RString, payload);
        return output.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Writes the globals substream with the given first-sheet offset, returning its bytes.
    /// </summary>
    /// <param name="options">The writer options.</param>
    /// <param name="codePage">The code page to declare.</param>
    /// <param name="globalsExtra">The extra-records callback.</param>
    /// <param name="sheets">The sheets.</param>
    /// <param name="substreams">The prebuilt sheet substreams.</param>
    /// <param name="firstSheetOffset">The offset of the first sheet substream.</param>
    /// <returns>The globals bytes.</returns>
    private static byte[] WriteGlobals(BiffWriterOptions options, int codePage, WriteAction? globalsExtra, (string Name, WriteAction Body)[] sheets, byte[][] substreams, long firstSheetOffset)
    {
        var output = new ArrayBufferWriter<byte>();
        var writer = new BiffWriter(output, options);
        writer.WriteBof(BiffSubstreamType.WorkbookGlobals);
        writer.WriteCodePage((ushort)codePage);
        globalsExtra?.Invoke(ref writer);

        long offset = firstSheetOffset;
        for (int i = 0; i < sheets.Length; i++)
        {
            writer.WriteBoundSheet((uint)offset, BiffSheetState.Visible, BiffSheetType.Worksheet, sheets[i].Name);
            offset += substreams[i].Length;
        }

        writer.WriteEof();
        return output.WrittenSpan.ToArray();
    }
}
