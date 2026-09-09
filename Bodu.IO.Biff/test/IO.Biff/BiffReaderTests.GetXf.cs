// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetXf.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that an XF record decodes its font, format, and type fields under both record lengths.
    /// </summary>
    /// <param name="length">The record length.</param>
    [TestMethod]
    [DataRow(16)]
    [DataRow(20)]
    public void GetXf_WhenWellFormed_ShouldDecodeLeadingFields(int length)
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Xf(2, 0x0E, 0xFFF5, length), BiffRecordType.Xf);

        BiffXfRecord xf = reader.GetXf();

        Assert.AreEqual(2, xf.FontIndex);
        Assert.AreEqual(0x0E, xf.FormatIndex);
        Assert.IsTrue(xf.IsLocked);
        Assert.IsFalse(xf.IsHidden);
        Assert.IsTrue(xf.IsStyle);
        Assert.AreEqual(0xFFF, xf.ParentStyleIndex);
    }

    /// <summary>
    /// Verifies that a cell XF with a parent style decodes its parent index.
    /// </summary>
    [TestMethod]
    public void GetXf_WhenCellFormat_ShouldDecodeParentStyleIndex()
    {
        BiffReader reader = ReadTo5(BiffTestRecords.Xf(0, 0, 0x0032, 16), BiffRecordType.Xf);

        BiffXfRecord xf = reader.GetXf();

        Assert.IsFalse(xf.IsStyle);
        Assert.IsTrue(xf.IsHidden);
        Assert.AreEqual(3, xf.ParentStyleIndex);
    }

    /// <summary>
    /// Verifies that an XF record carrying only its two indices, or the indices and one stray byte, is tolerated
    /// with a zero type field so the number format still resolves.
    /// </summary>
    /// <param name="length">The truncated payload length.</param>
    [TestMethod]
    [DataRow(4)]
    [DataRow(5)]
    public void GetXf_WhenRecordCarriesOnlyIndices_ShouldDecodeWithZeroTypeField(int length)
    {
        byte[] payload = new byte[length];
        payload[0] = 3;
        payload[2] = 0x0E;
        BiffReader reader = ReadTo8(BiffTestRecords.Record(BiffRecordType.Xf, payload), BiffRecordType.Xf);

        BiffXfRecord xf = reader.GetXf();

        Assert.AreEqual(3, xf.FontIndex);
        Assert.AreEqual(0x0E, xf.FormatIndex);
        Assert.AreEqual(0, xf.TypeField);
        Assert.IsFalse(xf.IsLocked);
        Assert.AreEqual(0, xf.ParentStyleIndex);
    }

    /// <summary>
    /// Verifies that every flag bit of the type field is decoded independently.
    /// </summary>
    /// <param name="typeField">The raw type field.</param>
    /// <param name="locked">The expected locked flag.</param>
    /// <param name="hidden">The expected hidden flag.</param>
    /// <param name="style">The expected style flag.</param>
    /// <param name="parent">The expected parent style index.</param>
    [TestMethod]
    [DataRow((ushort)0x0000, false, false, false, 0)]
    [DataRow((ushort)0x0001, true, false, false, 0)]
    [DataRow((ushort)0x0002, false, true, false, 0)]
    [DataRow((ushort)0x0004, false, false, true, 0)]
    [DataRow((ushort)0x0010, false, false, false, 1)]
    [DataRow((ushort)0xFFFF, true, true, true, 0xFFF)]
    public void GetXf_WhenTypeFieldBitsVary_ShouldDecodeEachFlag(ushort typeField, bool locked, bool hidden, bool style, int parent)
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Xf(0, 0, typeField), BiffRecordType.Xf);

        BiffXfRecord xf = reader.GetXf();

        Assert.AreEqual(locked, xf.IsLocked);
        Assert.AreEqual(hidden, xf.IsHidden);
        Assert.AreEqual(style, xf.IsStyle);
        Assert.AreEqual(parent, xf.ParentStyleIndex);
    }
}
