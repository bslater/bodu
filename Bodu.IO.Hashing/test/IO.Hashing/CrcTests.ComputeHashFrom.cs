// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcTests.ComputeHashFrom.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class CrcTests
{
    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHashFrom(System.ReadOnlySpan{byte}, System.ReadOnlySpan{byte})" />
    /// continues a prior hash by undoing finalisation and hashing additional data, yielding the same digest as
    /// a single-shot hash over the combined input.
    /// </summary>
    [TestMethod]
    public void ComputeHashFrom_WhenResumedWithAdditionalInput_ShouldMatchSingleShotCombinedHash()
    {
        byte[] first = new byte[] { 0x31, 0x32, 0x33, 0x34 };
        byte[] rest = new byte[] { 0x35, 0x36, 0x37, 0x38, 0x39 };

        byte[] firstHash = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(first);

        Crc resumer = new(CrcStandard.CRC32_ISOHDLC);
        byte[] resumed = resumer.ComputeHashFrom(firstHash, rest);

        byte[] combined = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(RevEngCheckInput);
        CollectionAssert.AreEqual(combined, resumed);
    }

    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHashFrom(byte[], byte[], int, int)" /> resumes from a prior digest
    /// across a sliced window of <paramref name="newData" /> and matches the single-shot hash over the combined
    /// <c>first + newData[offset..offset+length]</c> payload.
    /// </summary>
    [TestMethod]
    public void ComputeHashFrom_WhenOffsetLengthSlicesNewData_ShouldMatchFullCombinedHash()
    {
        byte[] first = new byte[] { 0x31, 0x32, 0x33, 0x34 };
        byte[] newData = new byte[] { 0xDE, 0xAD, 0x35, 0x36, 0x37, 0x38, 0x39, 0xBE, 0xEF };
        int offset = 2;
        int length = 5;

        byte[] firstHash = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(first);

        Crc resumer = new(CrcStandard.CRC32_ISOHDLC);
        byte[] resumed = resumer.ComputeHashFrom(firstHash, newData, offset, length);

        byte[] combined = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(RevEngCheckInput);
        CollectionAssert.AreEqual(combined, resumed);
    }

    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHashFrom(byte[], byte[])" /> throws
    /// <see cref="System.ArgumentNullException" /> with <c>ParamName</c> equal to <c>previousHash</c> when the
    /// prior-digest argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ComputeHashFrom_WhenPreviousHashByteArrayIsNull_ShouldThrowArgumentNullException()
    {
        Crc crc = new(CrcStandard.CRC32_ISOHDLC);
        byte[] newData = new byte[] { 0x01 };

        var ex = Assert.ThrowsExactly<System.ArgumentNullException>(
            () => crc.ComputeHashFrom(null!, newData));
        Assert.AreEqual("previousHash", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHashFrom(byte[], byte[])" /> throws
    /// <see cref="System.ArgumentNullException" /> with <c>ParamName</c> equal to <c>newData</c> when the
    /// additional-data argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ComputeHashFrom_WhenNewDataByteArrayIsNull_ShouldThrowArgumentNullException()
    {
        Crc crc = new(CrcStandard.CRC32_ISOHDLC);
        byte[] previousHash = new byte[crc.HashLengthInBytes];

        var ex = Assert.ThrowsExactly<System.ArgumentNullException>(
            () => crc.ComputeHashFrom(previousHash, null!));
        Assert.AreEqual("newData", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHashFrom(byte[], byte[], int, int)" /> throws
    /// <see cref="System.ArgumentNullException" /> with <c>ParamName</c> equal to <c>previousHash</c> when the
    /// prior-digest argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ComputeHashFrom_WhenPreviousHashIsNull_ForOffsetOverload_ShouldThrowArgumentNullException()
    {
        Crc crc = new(CrcStandard.CRC32_ISOHDLC);
        byte[] newData = new byte[] { 0x01, 0x02, 0x03 };

        var ex = Assert.ThrowsExactly<System.ArgumentNullException>(
            () => crc.ComputeHashFrom(null!, newData, 0, newData.Length));
        Assert.AreEqual("previousHash", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHashFrom(byte[], byte[], int, int)" /> throws
    /// <see cref="System.ArgumentNullException" /> with <c>ParamName</c> equal to <c>newData</c> when the
    /// additional-data argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ComputeHashFrom_WhenNewDataIsNull_ForOffsetOverload_ShouldThrowArgumentNullException()
    {
        Crc crc = new(CrcStandard.CRC32_ISOHDLC);
        byte[] previousHash = new byte[crc.HashLengthInBytes];

        var ex = Assert.ThrowsExactly<System.ArgumentNullException>(
            () => crc.ComputeHashFrom(previousHash, null!, 0, 0));
        Assert.AreEqual("newData", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Crc.TryComputeHashFrom" /> throws
    /// <see cref="System.ArgumentException" /> with <c>ParamName</c> equal to <c>previousHash</c> when the
    /// supplied prior digest is shorter than <see cref="Crc.HashLengthInBytes" />.
    /// </summary>
    [TestMethod]
    public void TryComputeHashFrom_WhenPreviousHashLengthIsTooShort_ShouldThrowArgumentException()
    {
        Crc crc = new(CrcStandard.CRC32_ISOHDLC);
        byte[] previousHash = new byte[crc.HashLengthInBytes - 1];
        byte[] newData = new byte[] { 0x01 };
        byte[] destination = new byte[crc.HashLengthInBytes];

        var ex = Assert.ThrowsExactly<System.ArgumentException>(
            () => crc.TryComputeHashFrom(previousHash, newData, destination, out _));
        Assert.AreEqual("previousHash", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Crc.TryComputeHashFrom" /> throws
    /// <see cref="System.ArgumentException" /> with <c>ParamName</c> equal to <c>previousHash</c> when the
    /// supplied prior digest is longer than <see cref="Crc.HashLengthInBytes" />.
    /// </summary>
    [TestMethod]
    public void TryComputeHashFrom_WhenPreviousHashLengthIsTooLong_ShouldThrowArgumentException()
    {
        Crc crc = new(CrcStandard.CRC32_ISOHDLC);
        byte[] previousHash = new byte[crc.HashLengthInBytes + 1];
        byte[] newData = new byte[] { 0x01 };
        byte[] destination = new byte[crc.HashLengthInBytes];

        var ex = Assert.ThrowsExactly<System.ArgumentException>(
            () => crc.TryComputeHashFrom(previousHash, newData, destination, out _));
        Assert.AreEqual("previousHash", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Crc.TryComputeHashFrom" /> returns <see langword="false" /> and writes zero bytes
    /// when <paramref name="destination" /> is smaller than <see cref="Crc.HashLengthInBytes" />.
    /// </summary>
    [TestMethod]
    public void TryComputeHashFrom_WhenDestinationIsTooSmall_ShouldReturnFalseAndWriteZeroBytes()
    {
        Crc crc = new(CrcStandard.CRC32_ISOHDLC);
        byte[] previousHash = new byte[crc.HashLengthInBytes];
        byte[] newData = new byte[] { 0x01, 0x02, 0x03 };
        byte[] destination = new byte[crc.HashLengthInBytes - 1];

        bool success = crc.TryComputeHashFrom(previousHash, newData, destination, out int written);

        Assert.IsFalse(success);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="Crc.TryComputeHashFrom" /> returns <see langword="true" /> and writes
    /// <see cref="Crc.HashLengthInBytes" /> bytes into <paramref name="destination" /> when the destination has
    /// exactly enough room.
    /// </summary>
    [TestMethod]
    public void TryComputeHashFrom_WhenDestinationIsExactSize_ShouldReturnTrueAndWriteExpectedHash()
    {
        Crc crc = new(CrcStandard.CRC32_ISOHDLC);
        byte[] first = new byte[] { 0x31, 0x32, 0x33, 0x34 };
        byte[] rest = new byte[] { 0x35, 0x36, 0x37, 0x38, 0x39 };
        byte[] firstHash = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(first);
        byte[] destination = new byte[crc.HashLengthInBytes];

        bool success = crc.TryComputeHashFrom(firstHash, rest, destination, out int written);

        Assert.IsTrue(success);
        Assert.AreEqual(crc.HashLengthInBytes, written);

        byte[] combined = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(RevEngCheckInput);
        CollectionAssert.AreEqual(combined, destination);
    }

    /// <summary>
    /// Verifies that resume semantics hold for a non-reflecting CRC (<c>CRC-16/XMODEM</c>), exercising the
    /// <c>ReflectIn == ReflectOut == false</c> branch inside <see cref="Crc.TryComputeHashFrom" /> where bit
    /// reversal is skipped.
    /// </summary>
    [TestMethod]
    public void ComputeHashFrom_WhenStandardIsNonReflecting_ShouldMatchSingleShotCombinedHash()
    {
        byte[] first = new byte[] { 0x31, 0x32, 0x33, 0x34 };
        byte[] rest = new byte[] { 0x35, 0x36, 0x37, 0x38, 0x39 };

        byte[] firstHash = new Crc(CrcStandard.CRC16_XMODEM).ComputeHash(first);

        Crc resumer = new(CrcStandard.CRC16_XMODEM);
        byte[] resumed = resumer.ComputeHashFrom(firstHash, rest);

        byte[] combined = new Crc(CrcStandard.CRC16_XMODEM).ComputeHash(RevEngCheckInput);
        CollectionAssert.AreEqual(combined, resumed);
    }

    /// <summary>
    /// Verifies that resume semantics hold for a CRC whose input reflection differs from its output reflection
    /// (<c>CRC-12/UMTS</c>, <c>ReflectIn=false, ReflectOut=true</c>), exercising the <c>ReflectIn ^ ReflectOut</c>
    /// branch in <see cref="Crc.TryComputeHashFrom" /> that applies bit reversal when undoing finalisation.
    /// </summary>
    [TestMethod]
    public void ComputeHashFrom_WhenReflectInDiffersFromReflectOut_ShouldMatchSingleShotCombinedHash()
    {
        CrcStandard asymmetric = CrcStandard.Get(CrcStandards.CRC12_UMTS);

        byte[] first = new byte[] { 0x31, 0x32, 0x33, 0x34 };
        byte[] rest = new byte[] { 0x35, 0x36, 0x37, 0x38, 0x39 };

        byte[] firstHash = new Crc(asymmetric).ComputeHash(first);

        Crc resumer = new(asymmetric);
        byte[] resumed = resumer.ComputeHashFrom(firstHash, rest);

        byte[] combined = new Crc(asymmetric).ComputeHash(RevEngCheckInput);
        CollectionAssert.AreEqual(combined, resumed);
    }
}
