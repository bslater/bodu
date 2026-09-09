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
}
