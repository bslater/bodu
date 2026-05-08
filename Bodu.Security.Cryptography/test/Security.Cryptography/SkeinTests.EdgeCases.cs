// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkeinTests.EdgeCases.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Probes the <see cref="Skein{T}" /> family for unexpected exceptions when its public surface
/// is exercised outside of expected usage — algorithm-name format, key length boundary
/// conditions, AlgorithmName disposed-state access, and idempotent disposal.
/// </summary>
public abstract partial class SkeinTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that <see cref="Skein{T}.AlgorithmName" /> returns the documented
    /// <c>"Skein-{state}-{hashSize}"</c> format using the algorithm's current configuration.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenReadOnDefaultInstance_ShouldFollowSkeinNamingConvention()
    {
        using var skein = new TAlgorithm();
        string name = skein.AlgorithmName;

        Assert.IsTrue(name.StartsWith("Skein-", StringComparison.Ordinal),
            $"Expected Skein algorithm name to start with 'Skein-', got '{name}'.");
        Assert.IsTrue(name.EndsWith("-" + skein.HashSize, StringComparison.Ordinal),
            $"Expected Skein algorithm name to end with '-{skein.HashSize}', got '{name}'.");
    }

    /// <summary>
    /// Verifies that reading <see cref="Skein{T}.AlgorithmName" /> after disposal throws
    /// <see cref="ObjectDisposedException" /> rather than returning a stale or partially-zeroed
    /// string.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenAccessedAfterDispose_ShouldThrowExactly()
    {
        var skein = new TAlgorithm();
        skein.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = skein.AlgorithmName;
        });
    }

    /// <summary>
    /// Verifies that assigning a <see cref="Skein{T}.Key" /> of exactly
    /// <see cref="Skein{T}.MaxKeySizeBytes" /> bytes succeeds (boundary value test).
    /// </summary>
    [TestMethod]
    public void Key_WhenAssignedExactlyMaxKeySize_ShouldNotThrow()
    {
        using var skein = new TAlgorithm();

        try
        {
            skein.Key = new byte[Skein<TAlgorithm>.MaxKeySizeBytes];
        }
        catch (Exception ex)
        {
            Assert.Fail(
                $"Assigning a {Skein<TAlgorithm>.MaxKeySizeBytes}-byte key should be the inclusive upper bound, " +
                $"but threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that calling <see cref="Skein{T}.Dispose" /> twice on the same instance is
    /// idempotent and does not throw — the constructor takes ownership of an internal Threefish
    /// cipher, and a double-dispose path could otherwise propagate an inner
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var skein = new TAlgorithm();
        skein.Dispose();

        try
        {
            skein.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Second Dispose on {typeof(TAlgorithm).Name} threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that disposing a freshly-constructed Skein instance — one that has never had any
    /// property accessed or hashing performed — completes without throwing. Regression guard for
    /// disposal paths that touch the internal Threefish cipher without null checks.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInstanceUntouched_ShouldNotThrow()
    {
        var skein = new TAlgorithm();

        try
        {
            skein.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Disposing an untouched {typeof(TAlgorithm).Name} instance threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that switching <see cref="Skein{T}.Key" /> from a non-empty value back to an empty
    /// array causes the next <see cref="HashAlgorithm.ComputeHash(byte[])" /> call to produce the
    /// canonical plain-hash digest — the empty array is the documented sentinel for the unkeyed
    /// profile and must not leave the algorithm in a stale Skein-MAC state.
    /// </summary>
    [TestMethod]
    public void Key_WhenReassignedFromMacToEmpty_ShouldRestorePlainHashOutput()
    {
        byte[] input = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray();

        byte[] plainHash;
        using (var plain = new TAlgorithm())
            plainHash = plain.ComputeHash(input);

        using var skein = new TAlgorithm { Key = SkeinTestKey };
        _ = skein.ComputeHash(input);

        skein.Key = Array.Empty<byte>();
        byte[] reverted = skein.ComputeHash(input);

        CollectionAssert.AreEqual(plainHash, reverted,
            "Reassigning Key to an empty array must restore the canonical plain-hash digest.");
    }
}
