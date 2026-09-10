// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.CodePage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that a CODEPAGE record updates the code page the reader reports.
    /// </summary>
    [TestMethod]
    public void CodePage_WhenCodePageRecordRead_ShouldUpdate()
    {
        var reader = new BiffReader(BiffTestRecords.Stream(BiffTestRecords.Bof5(), BiffTestRecords.CodePage(850)));

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BiffLimits.DefaultCodePage, reader.CodePage);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(850, reader.CodePage);
        Assert.AreEqual(850, reader.CurrentState.CodePage);
    }

    /// <summary>
    /// Verifies that the private Apple Roman and legacy ANSI markers are normalized to Windows code page numbers.
    /// </summary>
    /// <param name="raw">The raw CODEPAGE value.</param>
    /// <param name="expected">The normalized code page.</param>
    [TestMethod]
    [DataRow((ushort)0x8000, 10000)]
    [DataRow((ushort)0x8001, 1252)]
    [DataRow((ushort)0x04B0, 1200)]
    public void CodePage_WhenMarkerValue_ShouldNormalize(ushort raw, int expected)
    {
        var reader = new BiffReader(BiffTestRecords.CodePage(raw));

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(expected, reader.CodePage);
    }

    /// <summary>
    /// Verifies that a CODEPAGE record too short to carry a value is skipped during traversal and leaves the code page
    /// unchanged, while its accessor rejects it.
    /// </summary>
    [TestMethod]
    public void CodePage_WhenCodePageRecordIsTruncated_ShouldLeaveCodePageUnchanged()
    {
        var reader = new BiffReader(BiffTestRecords.Record(BiffRecordType.CodePage, [0x01]));

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BiffLimits.DefaultCodePage, reader.CodePage);
        _ = Assert.ThrowsExactly<BiffFormatException>(() =>
        {
            var again = new BiffReader(BiffTestRecords.Record(BiffRecordType.CodePage, [0x01]));
            _ = again.Read();
            _ = again.GetCodePage();
        });
    }

    /// <summary>
    /// Verifies that the code page in effect decodes BIFF5 byte strings.
    /// </summary>
    [TestMethod]
    public void CodePage_WhenBiff5LabelFollowsCodePage_ShouldDecodeWithThatCodePage()
    {
        // 0xE9 is 'é' in Windows-1252 and 'Θ' in DOS code page 437.
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof5(), BiffTestRecords.CodePage(437), BiffTestRecords.Label5(0, 0, [0x41, 0xE9]));
        BiffReader reader = ReadTo(stream, BiffRecordType.Label);

        Assert.AreEqual("AΘ", reader.GetLabel().Text.GetString());
    }

    /// <summary>
    /// Verifies that a zero-length CODEPAGE record is framed without changing the code page.
    /// </summary>
    [TestMethod]
    public void CodePage_WhenCodePageRecordIsEmpty_ShouldLeaveCodePageUnchanged()
    {
        var reader = new BiffReader(BiffTestRecords.Stream(BiffTestRecords.CodePage(850), BiffTestRecords.Record(BiffRecordType.CodePage, default)));

        Assert.IsTrue(reader.Read());
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(850, reader.CodePage);
    }

    /// <summary>
    /// Verifies that successive CODEPAGE records each replace the code page, so the most recent one governs the byte
    /// strings that follow it.
    /// </summary>
    [TestMethod]
    public void CodePage_WhenSeveralCodePageRecords_ShouldUseMostRecent()
    {
        byte[] stream = BiffTestRecords.Stream(
            BiffTestRecords.Bof5(),
            BiffTestRecords.CodePage(437),
            BiffTestRecords.Label5(0, 0, [0xE9]),
            BiffTestRecords.CodePage(1252),
            BiffTestRecords.Label5(0, 1, [0xE9]));
        var reader = new BiffReader(stream);
        var texts = new List<string>();

        while (reader.Read())
        {
            if (reader.RecordType == BiffRecordType.Label)
                texts.Add(reader.GetLabel().Text.GetString());
        }

        CollectionAssert.AreEqual(new[] { "Θ", "é" }, texts);
        Assert.AreEqual(1252, reader.CodePage);
    }

    /// <summary>
    /// Verifies that a CODEPAGE record declaring an unresolvable code page is accepted by the reader and only fails
    /// when a byte string is decoded with it.
    /// </summary>
    [TestMethod]
    public void CodePage_WhenCodePageIsUnresolvable_ShouldFailOnlyWhenDecoding()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof5(), BiffTestRecords.CodePage(12345), BiffTestRecords.Label5(0, 0, [0x41]));
        BiffReader reader = ReadTo(stream, BiffRecordType.Label);
        Assert.AreEqual(12345, reader.CodePage);
        BiffLabelRecord label = reader.GetLabel();
        Assert.AreEqual(1, label.Text.Length);

        _ = Assert.ThrowsExactly<BiffFormatException>(() =>
        {
            BiffReader again = ReadTo(stream, BiffRecordType.Label);
            _ = again.GetLabel().Text.GetString();
        });
    }

    /// <summary>
    /// Verifies that a BIFF8 stream's Unicode CODEPAGE value does not affect Unicode-string decoding.
    /// </summary>
    [TestMethod]
    public void CodePage_WhenBiff8DeclaresUnicode_ShouldDecodeUnicodeStringsUnchanged()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof8(), BiffTestRecords.CodePage(1200), BiffTestRecords.Label8(0, 0, "café"));
        BiffReader reader = ReadTo(stream, BiffRecordType.Label);

        Assert.AreEqual(1200, reader.CodePage);
        Assert.AreEqual("café", reader.GetLabel().Text.GetString());
        Assert.IsTrue(reader.GetLabel().Text.IsUnicode);
    }
}
