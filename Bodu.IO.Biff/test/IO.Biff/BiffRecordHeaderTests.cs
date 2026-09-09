// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffRecordHeaderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Tests for <see cref="BiffRecordHeader" />: parsing and writing the four-byte record header.
/// </summary>
[TestClass]
public sealed class BiffRecordHeaderTests
{
    /// <summary>
    /// Verifies that a header parses its little-endian identifier and length and maps the identifier to its type.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenFourBytesAvailable_ShouldParse()
    {
        Assert.IsTrue(BiffRecordHeader.TryParse([0x09, 0x08, 0x10, 0x00, 0xFF], out BiffRecordHeader header));

        Assert.AreEqual(0x0809, header.Id);
        Assert.AreEqual(16, header.Length);
        Assert.AreEqual(BiffRecordType.Bof, header.Type);
    }

    /// <summary>
    /// Verifies that fewer than four bytes cannot form a header.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenFewerThanFourBytes_ShouldReturnFalse()
    {
        Assert.IsFalse(BiffRecordHeader.TryParse([0x09, 0x08, 0x10], out BiffRecordHeader header));
        Assert.AreEqual(default, header);
    }

    /// <summary>
    /// Verifies that a header writes its little-endian bytes.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenBufferLargeEnough_ShouldWriteLittleEndian()
    {
        Span<byte> buffer = stackalloc byte[4];

        new BiffRecordHeader(0x0203, 14).WriteTo(buffer);

        CollectionAssert.AreEqual(new byte[] { 0x03, 0x02, 0x0E, 0x00 }, buffer.ToArray());
    }

    /// <summary>
    /// Verifies that a buffer shorter than the header is rejected.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenBufferTooSmall_ShouldThrowArgumentException()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            new BiffRecordHeader(1, 1).WriteTo(new byte[3]);
        });
    }
}
