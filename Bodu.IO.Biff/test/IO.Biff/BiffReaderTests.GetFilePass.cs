// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetFilePass.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that a BIFF5 FILEPASS record decodes as XOR with its key and hash.
    /// </summary>
    [TestMethod]
    public void GetFilePass_WhenBiff5_ShouldDecodeXorPair()
    {
        BiffReader reader = ReadTo5(BiffTestRecords.Record(BiffRecordType.FilePass, [0x34, 0x12, 0x78, 0x56]), BiffRecordType.FilePass);

        BiffFilePassRecord pass = reader.GetFilePass();

        Assert.AreEqual(BiffEncryptionType.Xor, pass.EncryptionType);
        Assert.AreEqual(0x1234, pass.XorKey);
        Assert.AreEqual(0x5678, pass.XorHash);
    }

    /// <summary>
    /// Verifies that a BIFF8 XOR FILEPASS record decodes the pair that follows its type word.
    /// </summary>
    [TestMethod]
    public void GetFilePass_WhenBiff8Xor_ShouldDecodeXorPair()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Record(BiffRecordType.FilePass, [0x00, 0x00, 0x34, 0x12, 0x78, 0x56]), BiffRecordType.FilePass);

        BiffFilePassRecord pass = reader.GetFilePass();

        Assert.AreEqual(BiffEncryptionType.Xor, pass.EncryptionType);
        Assert.AreEqual(0x1234, pass.XorKey);
        Assert.AreEqual(0x5678, pass.XorHash);
    }

    /// <summary>
    /// Verifies that a BIFF8 RC4 FILEPASS record reports RC4 and no XOR parameters.
    /// </summary>
    [TestMethod]
    public void GetFilePass_WhenBiff8Rc4_ShouldReportRc4()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Record(BiffRecordType.FilePass, [0x01, 0x00, 0x01, 0x00, 0x01, 0x00]), BiffRecordType.FilePass);

        BiffFilePassRecord pass = reader.GetFilePass();

        Assert.AreEqual(BiffEncryptionType.Rc4, pass.EncryptionType);
        Assert.AreEqual(0, pass.XorKey);
    }

    /// <summary>
    /// Verifies that a BIFF8 record declaring a scheme the codec does not name reports that scheme with zero XOR
    /// parameters rather than failing.
    /// </summary>
    [TestMethod]
    public void GetFilePass_WhenBiff8TypeIsUnknown_ShouldPreserveRawValueWithZeroParameters()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Record(BiffRecordType.FilePass, [0x05, 0x00, 0x34, 0x12]), BiffRecordType.FilePass);

        BiffFilePassRecord pass = reader.GetFilePass();

        Assert.AreEqual((BiffEncryptionType)5, pass.EncryptionType);
        Assert.AreEqual(0, pass.XorKey);
        Assert.AreEqual(0, pass.XorHash);
    }

    /// <summary>
    /// Verifies that a BIFF8 XOR record too short for its key pair is rejected.
    /// </summary>
    [TestMethod]
    public void GetFilePass_WhenBiff8XorIsTruncated_ShouldThrowBiffFormatException()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof8(), BiffTestRecords.Record(BiffRecordType.FilePass, [0x00, 0x00, 0x34, 0x12]));

        _ = Assert.ThrowsExactly<BiffFormatException>(() =>
        {
            BiffReader reader = ReadTo(stream, BiffRecordType.FilePass);
            _ = reader.GetFilePass();
        });
    }

    /// <summary>
    /// Verifies that a BIFF8 RC4 record carrying only its type word is accepted, since the RC4 header is not
    /// interpreted.
    /// </summary>
    [TestMethod]
    public void GetFilePass_WhenBiff8Rc4CarriesOnlyType_ShouldReportRc4()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Record(BiffRecordType.FilePass, [0x01, 0x00]), BiffRecordType.FilePass);

        Assert.AreEqual(BiffEncryptionType.Rc4, reader.GetFilePass().EncryptionType);
    }
}
