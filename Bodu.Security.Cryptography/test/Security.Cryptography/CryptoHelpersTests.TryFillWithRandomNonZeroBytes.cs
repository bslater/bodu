// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.TryFillWithRandomNonZeroBytes.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.TryFillWithRandomNonZeroBytes" /> returns true and fills span with non-zero bytes.
    /// </summary>
    [TestMethod]
    public void TryFillWithRandomNonZeroBytes_WhenBufferCanBeFilled_ShouldReturnTrue()
    {
        Span<byte> span = stackalloc byte[32];
        bool result = CryptoHelpers.TryFillWithRandomNonZeroBytes(span);
        Assert.IsTrue(result);
        foreach (byte b in span)
        {
            Assert.AreNotEqual(0, b);
        }
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.TryFillWithRandomNonZeroBytes" />, when Successful, returns a value that differs from the baseline.
    /// </summary>
    [TestMethod]
    public void TryFillWithRandomNonZeroBytes_WhenSuccessful_ShouldReturnTrueAndFillBuffer()
    {
        // Repeated to knock down flakiness; also guards against the NETSTANDARD2_0
        // branch regressing to never returning true on success.
        for (int attempt = 0; attempt < 100; attempt++)
        {
            byte[] buffer = new byte[32];
            bool result = CryptoHelpers.TryFillWithRandomNonZeroBytes(buffer.AsSpan());

            Assert.IsTrue(result, "TryFillWithRandomNonZeroBytes should return true on success.");

            for (int i = 0; i < buffer.Length; i++)
            {
                Assert.AreNotEqual((byte)0, buffer[i], "No byte in the filled buffer should be zero.");
            }
        }
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.TryFillWithRandomNonZeroBytes" />, RepeatedCalls, returns a value that differs from the baseline.
    /// </summary>
    [TestMethod]
    public void TryFillWithRandomNonZeroBytes_RepeatedCalls_ShouldProduceIndependentDraws()
    {
        // Behavioural guard for the NETSTANDARD2_0 finally-block that clears the
        // internal `temp` buffer after copying. If the clear were to corrupt the
        // result on the success path, the first and second draws would produce
        // identical buffers (or one would contain zeros). We assert neither.
        byte[] first = new byte[64];
        byte[] second = new byte[64];

        Assert.IsTrue(CryptoHelpers.TryFillWithRandomNonZeroBytes(first.AsSpan()));
        Assert.IsTrue(CryptoHelpers.TryFillWithRandomNonZeroBytes(second.AsSpan()));

        for (int i = 0; i < first.Length; i++)
            Assert.AreNotEqual((byte)0, first[i]);
        for (int i = 0; i < second.Length; i++)
            Assert.AreNotEqual((byte)0, second[i]);

        bool identical = true;
        for (int i = 0; i < first.Length; i++)
        {
            if (first[i] != second[i])
            {
                identical = false;
                break;
            }
        }
        Assert.IsFalse(identical, "Consecutive calls should not produce identical buffers; temp-clear must not corrupt the output path.");
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.TryFillWithRandomNonZeroBytes" />, BufferBytes, returns a value that differs from the baseline.
    /// </summary>
    [TestMethod]
    public void TryFillWithRandomNonZeroBytes_BufferBytes_ShouldNotContainZero()
    {
        byte[] buffer = new byte[64];
        bool result = CryptoHelpers.TryFillWithRandomNonZeroBytes(buffer.AsSpan());

        Assert.IsTrue(result);
        for (int i = 0; i < buffer.Length; i++)
        {
            Assert.AreNotEqual((byte)0, buffer[i]);
        }
    }
}
