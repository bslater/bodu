// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffSstReaderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Tests for <see cref="BiffSstReader" />: reading the shared string table across its continuation records.
/// Member-specific tests live in the sibling partial files.
/// </summary>
[TestClass]
public sealed partial class BiffSstReaderTests
{
    /// <summary>The compressed-character flags byte.</summary>
    private const byte Compressed = 0x00;

    /// <summary>The 16-bit-character flags byte.</summary>
    private const byte Wide = 0x01;

    /// <summary>
    /// Builds a BIFF8 stream holding a BOF, the given SST and CONTINUE records, and an EOF.
    /// </summary>
    /// <param name="records">The SST record followed by its continuation records.</param>
    /// <returns>The stream bytes.</returns>
    private static byte[] Table(params byte[][] records) =>
        BiffTestRecords.Stream([BiffTestRecords.Bof8(), .. records, BiffTestRecords.Eof()]);

    /// <summary>
    /// Reads every string of the table in the stream, returning them in order.
    /// </summary>
    /// <param name="stream">The stream bytes.</param>
    /// <param name="bytesConsumed">When this method returns, the parent reader's position after the table.</param>
    /// <returns>The decoded strings.</returns>
    private static List<string> ReadAll(byte[] stream, out int bytesConsumed)
    {
        var reader = new BiffReader(stream);
        while (reader.Read() && reader.RecordType != BiffRecordType.Sst)
        {
        }

        Assert.AreEqual(BiffRecordType.Sst, reader.RecordType);
        var strings = new BiffSstReader(ref reader);
        var result = new List<string>();
        while (strings.Read(ref reader))
            result.Add(strings.GetString());

        bytesConsumed = reader.BytesConsumed;
        return result;
    }
}
