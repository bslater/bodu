// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WhirlpoolTests.EdgeCases.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Probes <see cref="Whirlpool" /> for unexpected exceptions when its public surface is exercised
/// outside of expected usage — idempotent disposal, untouched-instance disposal, and reuse
/// across <see cref="HashAlgorithm.ComputeHash(byte[])" /> calls. Complements
/// <c>WhirlpoolTests.Version</c> which already covers the algorithm-specific
/// <see cref="Whirlpool.Version" /> property.
/// </summary>
public partial class WhirlpoolTests
{
    /// <summary>
    /// Verifies that calling <see cref="Whirlpool.Dispose" /> twice on the same instance is
    /// idempotent and does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var algorithm = new Whirlpool();
        algorithm.Dispose();

        try
        {
            algorithm.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Second Dispose on Whirlpool threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that disposing a freshly-constructed <see cref="Whirlpool" /> instance — one that
    /// has never had any property accessed or hashing performed — completes without throwing.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInstanceUntouched_ShouldNotThrow()
    {
        var algorithm = new Whirlpool();

        try
        {
            algorithm.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Disposing an untouched Whirlpool instance threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.Initialize" /> on a disposed
    /// <see cref="Whirlpool" /> instance throws <see cref="ObjectDisposedException" /> rather than
    /// touching the cleared internal state.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var algorithm = new Whirlpool();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.Initialize();
        });
    }

    /// <summary>
    /// Verifies that two consecutive <see cref="HashAlgorithm.ComputeHash(byte[])" /> calls
    /// against the same <see cref="Whirlpool" /> instance produce identical digests for identical
    /// input — guarding against state leakage across reuse and confirming that
    /// <see cref="Whirlpool.Version" /> selection persists across hashes.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInvokedRepeatedly_ShouldYieldIdenticalDigest()
    {
        using var algorithm = new Whirlpool();
        byte[] input = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

        byte[] first = algorithm.ComputeHash(input);
        byte[] second = algorithm.ComputeHash(input);

        CollectionAssert.AreEqual(first, second);
    }
}
