// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconHash256Tests.EdgeCases.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Probes <see cref="AsconHash256" /> for unexpected exceptions when its public surface is
/// exercised outside of expected usage — disposed-state access on the unique
/// <see cref="AsconHash{T}.AlgorithmName" /> property, idempotent disposal, and reuse semantics.
/// </summary>
public partial class AsconHash256Tests
{
    /// <summary>
    /// Verifies that reading <see cref="AsconHash{T}.AlgorithmName" /> after disposal throws
    /// <see cref="ObjectDisposedException" /> rather than returning the stored algorithm name
    /// from a disposed instance.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenAccessedAfterDispose_ShouldThrowExactly()
    {
        var algorithm = new AsconHash256();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.AlgorithmName;
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="AsconHash256.Dispose" /> twice on the same instance is
    /// idempotent and does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var algorithm = new AsconHash256();
        algorithm.Dispose();

        try
        {
            algorithm.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Second Dispose on AsconHash256 threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that disposing a freshly-constructed <see cref="AsconHash256" /> instance — one
    /// that has never had any property accessed or hashing performed — completes without throwing.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInstanceUntouched_ShouldNotThrow()
    {
        var algorithm = new AsconHash256();

        try
        {
            algorithm.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Disposing an untouched AsconHash256 instance threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that two consecutive <see cref="HashAlgorithm.ComputeHash(byte[])" /> calls
    /// against the same <see cref="AsconHash256" /> instance produce identical digests for
    /// identical input — guarding against state leakage across reuse.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInvokedRepeatedly_ShouldYieldIdenticalDigest()
    {
        using var algorithm = new AsconHash256();
        byte[] input = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

        byte[] first = algorithm.ComputeHash(input);
        byte[] second = algorithm.ComputeHash(input);

        CollectionAssert.AreEqual(first, second);
    }

    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.Initialize" /> on a disposed
    /// <see cref="AsconHash256" /> instance throws <see cref="ObjectDisposedException" /> rather
    /// than touching the cleared internal state.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var algorithm = new AsconHash256();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.Initialize();
        });
    }
}
