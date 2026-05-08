// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3Tests.EdgeCases.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Probes <see cref="Blake3" /> for unexpected exceptions when its public surface is exercised
/// outside of expected usage — idempotent disposal, untouched-instance disposal, and reuse
/// across multiple <see cref="HashAlgorithm.ComputeHash(byte[])" /> calls.
/// </summary>
public partial class Blake3Tests
{
    /// <summary>
    /// Verifies that calling <see cref="Blake3.Dispose" /> twice on the same instance is
    /// idempotent and does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var algorithm = new Blake3();
        algorithm.Dispose();

        try
        {
            algorithm.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Second Dispose on Blake3 threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that disposing a freshly-constructed <see cref="Blake3" /> instance — one that
    /// has never had any property accessed or hashing performed — completes without throwing.
    /// Regression guard for disposal paths that touch lazily-initialised state without null
    /// checks.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInstanceUntouched_ShouldNotThrow()
    {
        var algorithm = new Blake3();

        try
        {
            algorithm.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Disposing an untouched Blake3 instance threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that two consecutive <see cref="HashAlgorithm.ComputeHash(byte[])" /> calls
    /// against the same <see cref="Blake3" /> instance produce identical digests for identical
    /// input — guarding against state leakage across reuse.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInvokedRepeatedly_ShouldYieldIdenticalDigest()
    {
        using var algorithm = new Blake3();
        byte[] input = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

        byte[] first = algorithm.ComputeHash(input);
        byte[] second = algorithm.ComputeHash(input);

        CollectionAssert.AreEqual(first, second);
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.Initialize" /> on a disposed <see cref="Blake3" />
    /// instance throws <see cref="ObjectDisposedException" /> rather than touching cleared
    /// internal state.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var algorithm = new Blake3();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.Initialize();
        });
    }
}
