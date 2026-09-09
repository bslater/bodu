// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.IO.Biff;

/// <summary>
/// Tests for <see cref="BiffReader" />: record framing, version and code-page state, raw payload access, and the
/// typed accessors. Member-specific tests live in the sibling partial files.
/// </summary>
[TestClass]
public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Advances a reader over the specified stream until it is positioned on the first record of the given type.
    /// </summary>
    /// <param name="stream">The stream bytes.</param>
    /// <param name="type">The record type to stop on.</param>
    /// <returns>The reader, positioned on the record.</returns>
    private static BiffReader ReadTo(byte[] stream, BiffRecordType type)
    {
        var reader = new BiffReader(stream);
        while (reader.Read())
        {
            if (reader.RecordType == type)
                return reader;
        }

        Assert.Fail($"No {type} record found.");
        return reader;
    }

    /// <summary>
    /// Advances a reader over a BIFF8 stream — a BIFF8 BOF followed by the record — onto the record.
    /// </summary>
    /// <param name="record">The record bytes.</param>
    /// <param name="type">The record type to stop on.</param>
    /// <returns>The reader, positioned on the record.</returns>
    private static BiffReader ReadTo8(byte[] record, BiffRecordType type) =>
        ReadTo(BiffTestRecords.Stream(BiffTestRecords.Bof8(), record), type);

    /// <summary>
    /// Advances a reader over a BIFF5 stream — a BIFF5 BOF followed by the record — onto the record.
    /// </summary>
    /// <param name="record">The record bytes.</param>
    /// <param name="type">The record type to stop on.</param>
    /// <returns>The reader, positioned on the record.</returns>
    private static BiffReader ReadTo5(byte[] record, BiffRecordType type) =>
        ReadTo(BiffTestRecords.Stream(BiffTestRecords.Bof5(), record), type);

    /// <summary>
    /// Supplies streams whose framing is malformed.
    /// </summary>
    public static IEnumerable<object[]> MalformedFraming
    {
        get
        {
            yield return [new InvalidKat<byte[]>("one trailing byte", [0x09], typeof(BiffFormatException))];
            yield return [new InvalidKat<byte[]>("three trailing bytes", [0x09, 0x08, 0x10], typeof(BiffFormatException))];
            yield return [new InvalidKat<byte[]>("payload overruns buffer", [0x03, 0x02, 0x0E, 0x00, 0x01, 0x02], typeof(BiffFormatException))];
            yield return [new InvalidKat<byte[]>("record then short trailer", [.. BiffTestRecords.Eof(), 0x0A, 0x00], typeof(BiffFormatException))];
        }
    }

    /// <summary>
    /// Supplies known-record payloads that are too short for their layout, each preceded by a BIFF8 BOF.
    /// </summary>
    public static IEnumerable<object[]> TruncatedKnownRecords
    {
        get
        {
            yield return [new InvalidKat<byte[]>("NUMBER 13 bytes", Short(BiffRecordType.Number, 13), typeof(BiffFormatException))];
            yield return [new InvalidKat<byte[]>("RK 9 bytes", Short(BiffRecordType.Rk, 9), typeof(BiffFormatException))];
            yield return [new InvalidKat<byte[]>("LABELSST 9 bytes", Short(BiffRecordType.LabelSst, 9), typeof(BiffFormatException))];
            yield return [new InvalidKat<byte[]>("BOOLERR 7 bytes", Short(BiffRecordType.BoolErr, 7), typeof(BiffFormatException))];
            yield return [new InvalidKat<byte[]>("BLANK 5 bytes", Short(BiffRecordType.Blank, 5), typeof(BiffFormatException))];
            yield return [new InvalidKat<byte[]>("MULRK 5 bytes", Short(BiffRecordType.MulRk, 5), typeof(BiffFormatException))];
            yield return [new InvalidKat<byte[]>("MULRK ragged run", Short(BiffRecordType.MulRk, 9), typeof(BiffFormatException))];
            yield return [new InvalidKat<byte[]>("MULBLANK ragged run", Short(BiffRecordType.MulBlank, 7), typeof(BiffFormatException))];
            yield return [new InvalidKat<byte[]>("FORMULA 13 bytes", Short(BiffRecordType.Formula, 13), typeof(BiffFormatException))];
            yield return [new InvalidKat<byte[]>("ROW 15 bytes", Short(BiffRecordType.Row, 15), typeof(BiffFormatException))];
            yield return [new InvalidKat<byte[]>("XF 5 bytes", Short(BiffRecordType.Xf, 5), typeof(BiffFormatException))];
            yield return [new InvalidKat<byte[]>("DIMENSIONS 11 bytes", Short(BiffRecordType.Dimensions, 11), typeof(BiffFormatException))];
            yield return [new InvalidKat<byte[]>("BOUNDSHEET 6 bytes", Short(BiffRecordType.BoundSheet, 6), typeof(BiffFormatException))];
            yield return [new InvalidKat<byte[]>("LABEL text overruns", BiffTestRecords.Record(BiffRecordType.Label, [0, 0, 0, 0, 0, 0, 0x05, 0x00, 0x00, (byte)'a']), typeof(BiffFormatException))];
            yield return [new InvalidKat<byte[]>("STRING 2 bytes", Short(BiffRecordType.String, 2), typeof(BiffFormatException))];
            yield return [new InvalidKat<byte[]>("FORMAT 2 bytes", Short(BiffRecordType.Format, 2), typeof(BiffFormatException))];
            yield return [new InvalidKat<byte[]>("FONT 14 bytes", Short(BiffRecordType.Font, 14), typeof(BiffFormatException))];
            yield return [new InvalidKat<byte[]>("CODEPAGE 1 byte", Short(BiffRecordType.CodePage, 1), typeof(BiffFormatException))];
            yield return [new InvalidKat<byte[]>("DATEMODE 1 byte", Short(BiffRecordType.DateMode, 1), typeof(BiffFormatException))];
            yield return [new InvalidKat<byte[]>("SST 7 bytes", Short(BiffRecordType.Sst, 7), typeof(BiffFormatException))];
            yield return [new InvalidKat<byte[]>("FILEPASS 1 byte", Short(BiffRecordType.FilePass, 1), typeof(BiffFormatException))];
        }
    }

    /// <summary>
    /// Builds a record of the given type whose payload is all zeros of the given length.
    /// </summary>
    /// <param name="type">The record type.</param>
    /// <param name="length">The payload length.</param>
    /// <returns>The record.</returns>
    private static byte[] Short(BiffRecordType type, int length) =>
        BiffTestRecords.Record(type, new byte[length]);

    /// <summary>
    /// Invokes the typed accessor matching the current record so the truncation sweep can drive every decoder.
    /// </summary>
    /// <param name="reader">The reader positioned on the record.</param>
    private static void InvokeAccessor(ref BiffReader reader)
    {
        switch (reader.RecordType)
        {
            case BiffRecordType.Number: _ = reader.GetNumber(); break;
            case BiffRecordType.Rk: _ = reader.GetRk(); break;
            case BiffRecordType.LabelSst: _ = reader.GetLabelSst(); break;
            case BiffRecordType.BoolErr: _ = reader.GetBoolErr(); break;
            case BiffRecordType.Blank: _ = reader.GetBlank(); break;
            case BiffRecordType.MulRk: _ = reader.GetMulRk(); break;
            case BiffRecordType.MulBlank: _ = reader.GetMulBlank(); break;
            case BiffRecordType.Formula: _ = reader.GetFormula(); break;
            case BiffRecordType.Row: _ = reader.GetRow(); break;
            case BiffRecordType.Xf: _ = reader.GetXf(); break;
            case BiffRecordType.Dimensions: _ = reader.GetDimensions(); break;
            case BiffRecordType.BoundSheet: _ = reader.GetBoundSheet(); break;
            case BiffRecordType.Label: _ = reader.GetLabel(); break;
            case BiffRecordType.String: _ = reader.GetString(); break;
            case BiffRecordType.Format: _ = reader.GetFormat(); break;
            case BiffRecordType.Font: _ = reader.GetFont(); break;
            case BiffRecordType.CodePage: _ = reader.GetCodePage(); break;
            case BiffRecordType.DateMode: _ = reader.GetDateMode(); break;
            case BiffRecordType.Sst: _ = reader.GetSstHeader(); break;
            case BiffRecordType.FilePass: _ = reader.GetFilePass(); break;
            case BiffRecordType.Bof: _ = reader.GetBof(); break;
            default: Assert.Fail($"No accessor for {reader.RecordType}."); break;
        }
    }
}
