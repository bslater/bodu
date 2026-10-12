// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffStringRecordTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Tests for <see cref="BiffStringRecord" />: reading a formula's cached text across the CONTINUE records that carry
/// the rest of it. Member-specific tests live in the sibling partial files; the record's own decoding is covered by the
/// <c>GetString</c> tests of <see cref="BiffReaderTests" />.
/// </summary>
[TestClass]
public sealed partial class BiffStringRecordTests
{
    /// <summary>
    /// Reads the text of the first STRING record of a stream through <see cref="BiffStringRecord.ReadContinuedText" />.
    /// </summary>
    /// <param name="stream">The stream bytes.</param>
    /// <param name="bytesConsumed">When this method returns, the reader's position after the text.</param>
    /// <returns>The text.</returns>
    private static string ReadText(byte[] stream, out int bytesConsumed)
    {
        var reader = new BiffReader(stream);
        while (reader.Read() && reader.RecordType != BiffRecordType.String)
        {
        }

        Assert.AreEqual(BiffRecordType.String, reader.RecordType);
        string text = BiffStringRecord.ReadContinuedText(ref reader);
        bytesConsumed = reader.BytesConsumed;
        return text;
    }

    /// <summary>
    /// Builds a BIFF8 stream: a BOF, the given records and an EOF.
    /// </summary>
    /// <param name="records">The STRING record and the records after it.</param>
    /// <returns>The stream bytes.</returns>
    private static byte[] Stream8(params byte[][] records) =>
        BiffTestRecords.Stream([BiffTestRecords.Bof8(), .. records, BiffTestRecords.Eof()]);
}
