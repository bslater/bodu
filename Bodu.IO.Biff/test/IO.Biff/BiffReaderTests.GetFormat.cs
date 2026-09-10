// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetFormat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that a BIFF8 FORMAT record decodes its index and 16-bit-length code.
    /// </summary>
    [TestMethod]
    public void GetFormat_WhenBiff8_ShouldDecodeIndexAndCode()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Format8(164, "yyyy-mm-dd"), BiffRecordType.Format);

        BiffFormatRecord format = reader.GetFormat();

        Assert.AreEqual(164, format.FormatIndex);
        Assert.AreEqual("yyyy-mm-dd", format.Code.GetString());
    }

    /// <summary>
    /// Verifies that a BIFF5 FORMAT record decodes its index and 8-bit-length byte-string code.
    /// </summary>
    [TestMethod]
    public void GetFormat_WhenBiff5_ShouldDecodeByteStringCode()
    {
        BiffReader reader = ReadTo5(BiffTestRecords.Format5(5, [0x30, 0x2E, 0x30, 0x30]), BiffRecordType.Format);

        BiffFormatRecord format = reader.GetFormat();

        Assert.AreEqual(5, format.FormatIndex);
        Assert.AreEqual("0.00", format.Code.GetString());
    }

    /// <summary>
    /// Verifies that a BIFF8 code with a 16-bit length above 255 characters decodes, while the same length cannot
    /// occur under BIFF5's 8-bit prefix.
    /// </summary>
    [TestMethod]
    public void GetFormat_WhenBiff8CodeExceeds255Characters_ShouldDecode()
    {
        string code = new('0', 300);
        BiffReader reader = ReadTo8(BiffTestRecords.Format8(164, code), BiffRecordType.Format);

        Assert.AreEqual(code, reader.GetFormat().Code.GetString());
    }

    /// <summary>
    /// Verifies that a BIFF5 code is decoded with the declared code page.
    /// </summary>
    [TestMethod]
    public void GetFormat_WhenBiff5CodePageDeclared_ShouldDecodeCodeWithIt()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof5(), BiffTestRecords.CodePage(437), BiffTestRecords.Format5(164, [0x30, 0xE9]));
        BiffReader reader = ReadTo(stream, BiffRecordType.Format);

        BiffFormatRecord format = reader.GetFormat();

        Assert.AreEqual(164, format.FormatIndex);
        Assert.AreEqual("0Θ", format.Code.GetString());
    }

    /// <summary>
    /// Verifies that a BIFF8 record whose code is declared longer than the payload is rejected.
    /// </summary>
    [TestMethod]
    public void GetFormat_WhenCodeOverruns_ShouldThrowBiffFormatException()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof8(), BiffTestRecords.Record(BiffRecordType.Format, [0xA4, 0x00, 0x09, 0x00, 0x00, (byte)'0']));

        _ = Assert.ThrowsExactly<BiffFormatException>(() =>
        {
            BiffReader reader = ReadTo(stream, BiffRecordType.Format);
            _ = reader.GetFormat();
        });
    }
}
