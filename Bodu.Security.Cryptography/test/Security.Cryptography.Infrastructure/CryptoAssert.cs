// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoAssert.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Provides byte-oriented assertion helpers for cryptographic tests.
/// </summary>
/// <remarks>
/// The helpers improve failure diagnostics for byte-sequence comparisons (first differing index plus full hex
/// renderings). Exception assertions are intentionally not wrapped here — tests keep <c>Assert.ThrowsExactly</c> calls
/// explicit at the call site per repository convention.
/// </remarks>
internal static class CryptoAssert
{
    /// <summary>
    /// Asserts that two byte sequences are identical, reporting the first differing index and the full hex renderings
    /// of both operands on failure.
    /// </summary>
    /// <param name="expected">The expected bytes.</param>
    /// <param name="actual">The actual bytes produced by the code under test.</param>
    /// <param name="message">An optional context prefix included in the failure message.</param>
    public static void AreEqualBytes(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual, string? message = null)
    {
        if (expected.SequenceEqual(actual))
            return;

        var prefix = string.IsNullOrEmpty(message) ? string.Empty : message + " ";

        if (expected.Length != actual.Length)
        {
            Assert.Fail(
                $"{prefix}Byte sequences differ in length: expected {expected.Length} bytes but got {actual.Length}. " +
                $"Expected: <{Convert.ToHexString(expected)}> Actual: <{Convert.ToHexString(actual)}>");
        }

        var firstDifference = expected.CommonPrefixLength(actual);

        Assert.Fail(
            $"{prefix}Byte sequences differ at index {firstDifference} " +
            $"(expected 0x{expected[firstDifference]:X2}, actual 0x{actual[firstDifference]:X2}). " +
            $"Expected: <{Convert.ToHexString(expected)}> Actual: <{Convert.ToHexString(actual)}>");
    }
}
