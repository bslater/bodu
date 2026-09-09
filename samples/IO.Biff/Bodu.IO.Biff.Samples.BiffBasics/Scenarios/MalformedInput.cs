// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MalformedInput.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.IO.Biff.Samples.BiffBasics.Scenarios;

/// <summary>
/// Demonstrates the error contract: a truncated stream, a record whose payload overruns the buffer, and a known
/// record shorter than its layout are all <see cref="BiffFormatException" />; a stream in a BIFF version before
/// BIFF5 is <see cref="BiffUnsupportedVersionException" />; and an unknown record identifier is not an error at all.
/// </summary>
public static class MalformedInput
{
    /// <summary>
    /// Feeds several damaged inputs to the reader and prints how each is classified.
    /// </summary>
    /// <param name="stream">A well-formed workbook stream to damage.</param>
    public static void Run(byte[] stream)
    {
        Console.WriteLine("--- Malformed input ---");

        int cut = FindLongRecord(stream);

        // Cut the stream three bytes into a record header.
        Try("truncated header", stream.AsSpan(0, cut + 3).ToArray());

        // Cut the stream inside a payload.
        Try("payload overruns buffer", stream.AsSpan(0, cut + 6).ToArray());

        // A NUMBER record declaring only ten of its fourteen bytes: framed fine, rejected by the accessor.
        Try("truncated NUMBER payload", Frame(BiffRecordType.Number, new byte[10]));

        // A BIFF4 beginning-of-file record.
        Try("BIFF4 stream", Frame((BiffRecordType)0x0409, new byte[4]));

        // A record the codec does not name: readable through RecordId and ValueSpan, never an error.
        Try("unknown record", Frame((BiffRecordType)0x0FFE, [1, 2, 3]));

        // Feeding a stream in chunks: an incomplete trailing record is not an error when more data may follow.
        var chunked = new BiffReader(stream.AsSpan(0, cut + 6), isFinalBlock: false, default);
        int framed = 0;
        while (chunked.Read())
            framed++;
        Console.WriteLine($"  chunked read: {framed} complete record(s), {chunked.BytesConsumed} bytes consumed, waiting for more data");

        Console.WriteLine();
    }

    /// <summary>
    /// Reads every record of the bytes, decoding any NUMBER record, and reports the outcome.
    /// </summary>
    /// <param name="label">The scenario label.</param>
    /// <param name="bytes">The bytes to read.</param>
    private static void Try(string label, byte[] bytes)
    {
        try
        {
            var reader = new BiffReader(bytes);
            int count = 0;
            while (reader.Read())
            {
                count++;
                if (reader.RecordType == BiffRecordType.Number)
                    _ = reader.GetNumber();
            }

            Console.WriteLine($"  {label,-26}: ok, {count} record(s)");
        }
        catch (BiffUnsupportedVersionException ex)
        {
            Console.WriteLine($"  {label,-26}: unsupported version (marker 0x{ex.RawVersion:X4})");
        }
        catch (BiffFormatException ex)
        {
            Console.WriteLine($"  {label,-26}: format error at offset {ex.Offset?.ToString() ?? "n/a"} — {ex.Message}");
        }
    }

    /// <summary>
    /// Finds the offset of the first record after the BOF whose payload is long enough to cut inside.
    /// </summary>
    /// <param name="stream">The stream bytes.</param>
    /// <returns>The offset of that record's header.</returns>
    private static int FindLongRecord(byte[] stream)
    {
        var reader = new BiffReader(stream);
        while (reader.Read())
        {
            if (reader.RecordStartIndex > 0 && reader.RecordLength >= 8)
                return reader.RecordStartIndex;
        }

        throw new InvalidOperationException("No record long enough to cut.");
    }

    /// <summary>
    /// Frames a payload as a single record using the writer.
    /// </summary>
    /// <param name="type">The record type.</param>
    /// <param name="payload">The payload.</param>
    /// <returns>The framed record.</returns>
    private static byte[] Frame(BiffRecordType type, byte[] payload)
    {
        var output = new ArrayBufferWriter<byte>();
        var writer = new BiffWriter(output, BiffVersion.Biff8);
        writer.WriteRecord(type, payload);
        return output.WrittenSpan.ToArray();
    }
}
