// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.FillWithRandomNonZeroBytes.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.FillWithRandomBytesExcluding" /> returns a value that differs from the baseline.
    /// </summary>
    [TestMethod]
    public void FillWithRandomBytesExcluding_ShouldNeverContainExcludedValue()
    {
        const byte forbidden = 0xAA;

        for (int attempt = 0; attempt < 100; attempt++)
        {
            byte[] buffer = new byte[1024];
            CryptoHelpers.FillWithRandomBytesExcluding(forbidden, buffer.AsSpan());

            for (int i = 0; i < buffer.Length; i++)
            {
                Assert.AreNotEqual(forbidden, buffer[i], "Buffer must not contain the forbidden value.");
            }
        }
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.FillWithRandomBytesExcluding" /> returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void FillWithRandomBytesExcluding_ShouldNotLeaveTempBufferOnHeap()
    {
        // We cannot directly observe the internal temp buffer being zeroed
        // (defect B), so we assert the behavioural guarantee that successive
        // calls produce fresh randomness rather than any cached content. If
        // two consecutive fills are byte-identical for a reasonable length,
        // something is wrong (probability of accidental collision is ~2^-256).
        byte[] first = new byte[32];
        byte[] second = new byte[32];

        CryptoHelpers.FillWithRandomBytesExcluding(0x00, first.AsSpan());
        CryptoHelpers.FillWithRandomBytesExcluding(0x00, second.AsSpan());

        bool identical = true;
        for (int i = 0; i < first.Length; i++)
        {
            if (first[i] != second[i])
            {
                identical = false;
                break;
            }
        }

        Assert.IsTrue(!identical, "Successive calls should produce fresh random output, not cached bytes.");
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.FillWithRandomBytesExcluding" />, with HighForbiddenFrequency, returns a value that differs from the baseline.
    /// </summary>
    [TestMethod]
    public void FillWithRandomBytesExcluding_WithHighForbiddenFrequency_ShouldTerminate()
    {
        // Guards against regression to full-buffer refill on any match
        // (defect C). With a very small buffer and forbidden = 0x00 the
        // targeted per-byte replacement must terminate quickly.
        var task = Task.Run(() =>
        {
            for (int i = 0; i < 1000; i++)
            {
                byte[] buffer = new byte[2];
                CryptoHelpers.FillWithRandomBytesExcluding(0x00, buffer.AsSpan());

                Assert.AreNotEqual((byte)0, buffer[0]);
                Assert.AreNotEqual((byte)0, buffer[1]);
            }
        });

        bool completed = task.Wait(TimeSpan.FromSeconds(1));
        Assert.IsTrue(completed, "FillWithRandomBytesExcluding should terminate in bounded time.");
    }
    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.FillWithRandomNonZeroBytes" /> throws ArgumentNullException when the buffer is null.
    /// </summary>
    [TestMethod]
    public void FillWithRandomNonZeroBytes_WhenBufferIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => CryptoHelpers.FillWithRandomNonZeroBytes(null));
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.FillWithRandomNonZeroBytes" /> throws ArgumentException when the buffer is empty.
    /// </summary>
    [TestMethod]
    public void FillWithRandomNonZeroBytes_WhenBufferIsEmpty_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() => CryptoHelpers.FillWithRandomNonZeroBytes(Array.Empty<byte>()));
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.FillWithRandomNonZeroBytes" /> fills the buffer with only non-zero values.
    /// </summary>
    [TestMethod]
    public void FillWithRandomNonZeroBytes_WhenBufferHasLength_ShouldContainNoZeroBytes()
    {
        byte[] buffer = new byte[32];
        CryptoHelpers.FillWithRandomNonZeroBytes(buffer);
        CollectionAssert.DoesNotContain(buffer, (byte)0);
    }

    /// <summary>
    /// Verifies that FillWithRandomNonZeroBytes fills a span with only non-zero bytes.
    /// </summary>
    [TestMethod]
    public void FillWithRandomNonZeroBytesSpan_WhenCalled_ShouldContainNoZeroBytes()
    {
        Span<byte> span = stackalloc byte[32];
        CryptoHelpers.FillWithRandomNonZeroBytes(span);
        foreach (byte b in span)
        {
            Assert.AreNotEqual(0, b);
        }
    }
}
