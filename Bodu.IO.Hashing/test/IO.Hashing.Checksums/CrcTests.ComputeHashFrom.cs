// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcTests.ComputeHashFrom.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

public partial class CrcTests
{

    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHashFrom(byte[], byte[])" /> throws
    /// <see cref="System.ArgumentNullException" /> with <c>ParamName</c> equal to <c>newData</c> when the
    /// additional-data argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ComputeHashFrom_WhenNewDataByteArrayIsNull_ShouldThrowExactly()
    {
        Crc crc = new(CrcStandard.CRC32_ISOHDLC);
        var previousHash = new byte[crc.HashLengthInBytes];

        ArgumentNullException ex = Assert.ThrowsExactly<System.ArgumentNullException>(
            () => crc.ComputeHashFrom(previousHash, null!));
        Assert.AreEqual("newData", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHashFrom(byte[], byte[], int, int)" /> throws
    /// <see cref="System.ArgumentNullException" /> with <c>ParamName</c> equal to <c>newData</c> when the
    /// additional-data argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ComputeHashFrom_WhenNewDataIsNull_ForOffsetOverload_ShouldThrowExactly()
    {
        Crc crc = new(CrcStandard.CRC32_ISOHDLC);
        var previousHash = new byte[crc.HashLengthInBytes];

        ArgumentNullException ex = Assert.ThrowsExactly<System.ArgumentNullException>(
            () => crc.ComputeHashFrom(previousHash, null!, 0, 0));
        Assert.AreEqual("newData", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHashFrom(byte[], byte[], int, int)" /> resumes from a prior digest
    /// across a sliced window of <paramref name="newData" /> and matches the single-shot hash over the combined
    /// <c>first + newData[offset..offset+length]</c> payload.
    /// </summary>
    [TestMethod]
    public void ComputeHashFrom_WhenOffsetLengthSlicesNewData_ShouldMatchFullCombinedHash()
    {
        byte[] first = [0x31, 0x32, 0x33, 0x34];
        byte[] newData = [0xDE, 0xAD, 0x35, 0x36, 0x37, 0x38, 0x39, 0xBE, 0xEF];
        var offset = 2;
        var length = 5;

        var firstHash = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(first);

        Crc resumer = new(CrcStandard.CRC32_ISOHDLC);
        var resumed = resumer.ComputeHashFrom(firstHash, newData, offset, length);

        var combined = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(s_revEngCheckInput);
        CollectionAssert.AreEqual(combined, resumed);
    }

    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHashFrom(byte[], byte[], int, int)" /> throws
    /// <see cref="System.ArgumentOutOfRangeException" /> with <c>ParamName</c> equal to <c>offset</c> when the
    /// offset is negative, rather than surfacing a span-internal exception from the slice operation.
    /// </summary>
    [TestMethod]
    public void ComputeHashFrom_WhenOffsetIsNegative_ShouldThrowExactly()
    {
        Crc crc = new(CrcStandard.CRC32_ISOHDLC);
        var previousHash = new byte[crc.HashLengthInBytes];
        byte[] newData = [0x01, 0x02, 0x03, 0x04];

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<System.ArgumentOutOfRangeException>(
            () => crc.ComputeHashFrom(previousHash, newData, -1, 4));
        Assert.AreEqual("offset", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHashFrom(byte[], byte[], int, int)" /> throws
    /// <see cref="System.ArgumentOutOfRangeException" /> with <c>ParamName</c> equal to <c>length</c> when the
    /// length is negative.
    /// </summary>
    [TestMethod]
    public void ComputeHashFrom_WhenLengthIsNegative_ShouldThrowExactly()
    {
        Crc crc = new(CrcStandard.CRC32_ISOHDLC);
        var previousHash = new byte[crc.HashLengthInBytes];
        byte[] newData = [0x01, 0x02, 0x03, 0x04];

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<System.ArgumentOutOfRangeException>(
            () => crc.ComputeHashFrom(previousHash, newData, 0, -1));
        Assert.AreEqual("length", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHashFrom(byte[], byte[], int, int)" /> throws
    /// <see cref="System.ArgumentException" /> when <c>offset + length</c> exceeds the bounds of
    /// <paramref name="newData" />, rather than surfacing a span-internal slicing exception.
    /// </summary>
    [TestMethod]
    public void ComputeHashFrom_WhenOffsetPlusLengthExceedsNewData_ShouldThrowExactly()
    {
        Crc crc = new(CrcStandard.CRC32_ISOHDLC);
        var previousHash = new byte[crc.HashLengthInBytes];
        byte[] newData = [0x01, 0x02, 0x03, 0x04];

        Assert.ThrowsExactly<System.ArgumentException>(
            () => crc.ComputeHashFrom(previousHash, newData, 3, 4));
    }

    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHashFrom(byte[], byte[])" /> throws
    /// <see cref="System.ArgumentNullException" /> with <c>ParamName</c> equal to <c>previousHash</c> when the
    /// prior-digest argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ComputeHashFrom_WhenPreviousHashByteArrayIsNull_ShouldThrowExactly()
    {
        Crc crc = new(CrcStandard.CRC32_ISOHDLC);
        byte[] newData = [0x01];

        ArgumentNullException ex = Assert.ThrowsExactly<System.ArgumentNullException>(
            () => crc.ComputeHashFrom(null!, newData));
        Assert.AreEqual("previousHash", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHashFrom(byte[], byte[], int, int)" /> throws
    /// <see cref="System.ArgumentNullException" /> with <c>ParamName</c> equal to <c>previousHash</c> when the
    /// prior-digest argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ComputeHashFrom_WhenPreviousHashIsNull_ForOffsetOverload_ShouldThrowExactly()
    {
        Crc crc = new(CrcStandard.CRC32_ISOHDLC);
        byte[] newData = [0x01, 0x02, 0x03];

        ArgumentNullException ex = Assert.ThrowsExactly<System.ArgumentNullException>(
            () => crc.ComputeHashFrom(null!, newData, 0, newData.Length));
        Assert.AreEqual("previousHash", ex.ParamName);
    }
    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHashFrom(System.ReadOnlySpan{byte}, System.ReadOnlySpan{byte})" />
    /// continues a prior hash by undoing finalisation and hashing additional data, yielding the same digest as
    /// a single-shot hash over the combined input. Covers each reflection branch:
    /// <list type="bullet">
    ///   <item><c>CRC32_ISOHDLC</c> — fully reflecting (<c>ReflectIn = ReflectOut = true</c>).</item>
    ///   <item><c>CRC16_XMODEM</c> — non-reflecting (<c>ReflectIn = ReflectOut = false</c>).</item>
    ///   <item><c>CRC12_UMTS</c> — asymmetric (<c>ReflectIn != ReflectOut</c>) so the bit-reversal branch in
    ///   <see cref="Crc.TryComputeHashFrom" /> is exercised.</item>
    /// </list>
    /// </summary>
    /// <param name="standardId">The CRC catalogue entry under test.</param>
    [TestMethod]
    [DataRow(CrcStandards.CRC32_ISOHDLC)]
    [DataRow(CrcStandards.CRC16_XMODEM)]
    [DataRow(CrcStandards.CRC12_UMTS)]
    public void ComputeHashFrom_WhenResumed_ShouldMatchSingleShotCombinedHash(CrcStandards standardId)
    {
        var standard = CrcStandard.Get(standardId);

        byte[] first = [0x31, 0x32, 0x33, 0x34];
        byte[] rest = [0x35, 0x36, 0x37, 0x38, 0x39];

        var firstHash = new Crc(standard).ComputeHash(first);

        Crc resumer = new(standard);
        var resumed = resumer.ComputeHashFrom(firstHash, rest);

        var combined = new Crc(standard).ComputeHash(s_revEngCheckInput);
        CollectionAssert.AreEqual(combined, resumed);
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
        byte[] first = [0x31, 0x32, 0x33, 0x34];
        byte[] rest = [0x35, 0x36, 0x37, 0x38, 0x39];
        var firstHash = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(first);
        var destination = new byte[crc.HashLengthInBytes];

        var success = crc.TryComputeHashFrom(firstHash, rest, destination, out var written);

        Assert.IsTrue(success);
        Assert.AreEqual(crc.HashLengthInBytes, written);

        var combined = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(s_revEngCheckInput);
        CollectionAssert.AreEqual(combined, destination);
    }

    /// <summary>
    /// Verifies that <see cref="Crc.TryComputeHashFrom" /> returns <see langword="false" /> and writes zero bytes
    /// when <paramref name="destination" /> is smaller than <see cref="Crc.HashLengthInBytes" />.
    /// </summary>
    [TestMethod]
    public void TryComputeHashFrom_WhenDestinationIsTooSmall_ShouldReturnFalseAndWriteZeroBytes()
    {
        Crc crc = new(CrcStandard.CRC32_ISOHDLC);
        var previousHash = new byte[crc.HashLengthInBytes];
        byte[] newData = [0x01, 0x02, 0x03];
        var destination = new byte[crc.HashLengthInBytes - 1];

        var success = crc.TryComputeHashFrom(previousHash, newData, destination, out var written);

        Assert.IsFalse(success);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="Crc.TryComputeHashFrom" /> throws <see cref="System.ArgumentException" /> with
    /// <c>ParamName</c> equal to <c>previousHash</c> when the supplied prior digest is shorter or longer than
    /// <see cref="Crc.HashLengthInBytes" />.
    /// </summary>
    /// <param name="lengthDelta">The difference between the prior-hash length and <see cref="Crc.HashLengthInBytes" />.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(+1)]
    public void TryComputeHashFrom_WhenPreviousHashLengthMismatches_ShouldThrowExactly(int lengthDelta)
    {
        Crc crc = new(CrcStandard.CRC32_ISOHDLC);
        var previousHash = new byte[crc.HashLengthInBytes + lengthDelta];
        byte[] newData = [0x01];
        var destination = new byte[crc.HashLengthInBytes];

        ArgumentException ex = Assert.ThrowsExactly<System.ArgumentException>(
            () => crc.TryComputeHashFrom(previousHash, newData, destination, out _));
        Assert.AreEqual("previousHash", ex.ParamName);
    }

    /// <summary>
    /// Verifies that calling <see cref="Crc.ComputeHashFrom(byte[], byte[])" /> on an instance that already has
    /// in-progress incremental state does not disturb that state. After the resumption call returns, continuing
    /// to <see cref="Crc.Append" /> on the original instance must produce the same digest as a clean instance
    /// hashing the full concatenated input from scratch.
    /// </summary>
    [TestMethod]
    public void ComputeHashFrom_WhenInstanceHasInProgressIncrementalState_ShouldNotMutateThatState()
    {
        byte[] firstHalf = [0x31, 0x32];
        byte[] secondHalf = [0x33, 0x34];

        // Establish a clean baseline: hashing first+second in one go.
        Crc baseline = new(CrcStandard.CRC32_ISOHDLC);
        baseline.Append(firstHalf);
        baseline.Append(secondHalf);
        var baselineDigest = baseline.GetCurrentHash();

        Crc subject = new(CrcStandard.CRC32_ISOHDLC);
        subject.Append(firstHalf);

        // Resumption call that has no relationship to subject's incremental work.
        byte[] unrelatedPrevHash = [0xDE, 0xAD, 0xBE, 0xEF];
        byte[] unrelatedNewData = [0x99];
        _ = subject.ComputeHashFrom(unrelatedPrevHash, unrelatedNewData);

        // The subject's pending state must survive intact so the second half folds in correctly.
        subject.Append(secondHalf);
        var subjectDigest = subject.GetCurrentHash();

        CollectionAssert.AreEqual(baselineDigest, subjectDigest);
    }

    /// <summary>
    /// Verifies that <see cref="Crc.TryComputeHashFrom" /> leaves any in-progress incremental state on the
    /// instance intact when it returns <see langword="false" /> because <paramref name="destination" /> is too
    /// small to receive the finalized hash.
    /// </summary>
    [TestMethod]
    public void TryComputeHashFrom_WhenDestinationIsTooSmall_ShouldNotMutateIncrementalState()
    {
        byte[] firstHalf = [0x31, 0x32];
        byte[] secondHalf = [0x33, 0x34];

        // Establish a clean baseline.
        Crc baseline = new(CrcStandard.CRC32_ISOHDLC);
        baseline.Append(firstHalf);
        baseline.Append(secondHalf);
        var baselineDigest = baseline.GetCurrentHash();

        Crc subject = new(CrcStandard.CRC32_ISOHDLC);
        subject.Append(firstHalf);

        var previousHash = new byte[subject.HashLengthInBytes];
        var tooSmallDestination = new byte[subject.HashLengthInBytes - 1];
        var success = subject.TryComputeHashFrom(previousHash, [0xFF], tooSmallDestination, out var written);

        Assert.IsFalse(success);
        Assert.AreEqual(0, written);

        subject.Append(secondHalf);
        var subjectDigest = subject.GetCurrentHash();

        CollectionAssert.AreEqual(baselineDigest, subjectDigest);
    }

}
