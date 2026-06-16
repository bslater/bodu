// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmExtensionsTests.AppendData.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Tests for <see cref="HashAlgorithmExtensions.AppendData" />.
/// </summary>
public partial class HashAlgorithmExtensionsTests
{
    // ─── Argument validation ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.AppendData" /> throws
    /// <see cref="ArgumentNullException" /> when the algorithm argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AppendData_WhenAlgorithmIsNull_ShouldThrowExactly()
    {
        HashAlgorithm? algorithm = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
            algorithm!.AppendData(new ReadOnlySpan<byte>([1, 2, 3])));
    }

    // ─── Accumulation behaviour ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that an empty span does not alter the accumulated state.
    /// </summary>
    [TestMethod]
    public void AppendData_WhenSpanIsEmpty_ShouldNotContributeToHash()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();

        algorithm.AppendData([]);
        algorithm.TransformFinalBlock([], 0, 0);

        byte[] expected = BitConverter.GetBytes((uint)0);
        CollectionAssert.AreEqual(expected, algorithm.Hash);
    }

    /// <summary>
    /// Verifies that a single-byte span contributes exactly that byte value to the computed hash.
    /// </summary>
    [TestMethod]
    public void AppendData_WhenSpanHasSingleByte_ShouldHashCorrectly()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();

        algorithm.AppendData(new ReadOnlySpan<byte>([0xFF]));
        algorithm.TransformFinalBlock([], 0, 0);

        byte[] expected = BitConverter.GetBytes((uint)0xFF);
        CollectionAssert.AreEqual(expected, algorithm.Hash);
    }

    /// <summary>
    /// Verifies that the supplied bytes contribute to the final hash when the transform is
    /// subsequently finalised.
    /// </summary>
    [TestMethod]
    public void AppendData_WhenSpanContainsData_ShouldContributeToFinalHash()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        byte[] data = [1, 2, 3, 4];

        algorithm.AppendData(new ReadOnlySpan<byte>(data));
        algorithm.TransformFinalBlock([], 0, 0);

        // MonitoringHashAlgorithm accumulates input bytes as an unsigned 32-bit sum.
        byte[] expected = BitConverter.GetBytes((uint)(1 + 2 + 3 + 4));
        CollectionAssert.AreEqual(expected, algorithm.Hash);
    }

    /// <summary>
    /// Verifies that multiple calls accumulate all supplied bytes correctly in the final hash.
    /// </summary>
    [TestMethod]
    public void AppendData_WhenCalledMultipleTimes_ShouldAccumulateAllBytes()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();

        algorithm.AppendData(new ReadOnlySpan<byte>([10, 20]));
        algorithm.AppendData(new ReadOnlySpan<byte>([30, 40]));
        algorithm.TransformFinalBlock([], 0, 0);

        byte[] expected = BitConverter.GetBytes((uint)(10 + 20 + 30 + 40));
        CollectionAssert.AreEqual(expected, algorithm.Hash);
    }

    // ─── Equivalence & invocation tracking ────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.AppendData" /> produces a hash identical
    /// to <see cref="HashAlgorithm.ComputeHash(byte[])" /> when given the same input.
    /// </summary>
    [TestMethod]
    public void AppendData_WhenHashFinalised_ShouldMatchComputeHash()
    {
        byte[] data = [5, 10, 15, 20];

        byte[] expected;
        using (MonitoringHashAlgorithm reference = CreateAlgorithm())
            expected = reference.ComputeHash(data);

        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        algorithm.AppendData(new ReadOnlySpan<byte>(data));
        algorithm.TransformFinalBlock([], 0, 0);

        CollectionAssert.AreEqual(expected, algorithm.Hash);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithmExtensions.AppendData" /> increments the algorithm's
    /// <see cref="MonitoringHashAlgorithm.HashCoreCallCount" />, confirming the underlying
    /// <c>TransformBlock</c> path is exercised exactly once per call.
    /// </summary>
    [TestMethod]
    public void AppendData_WhenCalled_ShouldInvokeHashCoreOnce()
    {
        using MonitoringHashAlgorithm algorithm = CreateAlgorithm();
        int before = algorithm.HashCoreCallCount;

        algorithm.AppendData(new ReadOnlySpan<byte>([1, 2, 3]));

        Assert.AreEqual(before + 1, algorithm.HashCoreCallCount);
    }
}
