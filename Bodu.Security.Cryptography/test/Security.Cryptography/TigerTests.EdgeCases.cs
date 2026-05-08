// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TigerTests.EdgeCases.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Probes <see cref="Tiger" /> for unexpected exceptions when its public surface is exercised
/// outside of expected usage — disposed-state access, invalid enum casts, repeated disposal,
/// and state-machine transitions.
/// </summary>
public partial class TigerTests
{
    /// <summary>
    /// Verifies that reading <see cref="Tiger.AlgorithmName" /> after <see cref="HashAlgorithm.Dispose()" />
    /// throws <see cref="ObjectDisposedException" /> and not a generic <see cref="NullReferenceException" />.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenAccessedAfterDispose_ShouldThrowExactly()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.AlgorithmName;
        });
    }

    /// <summary>
    /// Verifies that reading <see cref="Tiger.HashSize" /> after disposal throws
    /// <see cref="ObjectDisposedException" /> rather than returning a stale value.
    /// </summary>
    [TestMethod]
    public void HashSize_WhenAccessedAfterDispose_ShouldThrowExactly()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.HashSize;
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.Initialize" /> on a disposed instance
    /// throws <see cref="ObjectDisposedException" /> and not <see cref="NullReferenceException" />
    /// from the residual buffer access in the base class.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.Initialize();
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="Tiger.Dispose" /> a second time is idempotent and does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        try
        {
            algorithm.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Second Dispose on Tiger threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that assigning an undefined <see cref="TigerHashingVariant" /> value to
    /// <see cref="Tiger.Variant" /> throws <see cref="ArgumentOutOfRangeException" /> via the
    /// enum-defined-value guard rather than a raw cast pass-through.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(2)]
    [DataRow(int.MaxValue)]
    [DataRow(int.MinValue)]
    public void Variant_WhenSetToUndefinedEnum_ShouldThrowExactly(int rawValue)
    {
        using var algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.Variant = (TigerHashingVariant)rawValue;
        });
    }

    /// <summary>
    /// Verifies that setting <see cref="Tiger.HashSize" /> after <see cref="HashAlgorithm.TransformBlock" />
    /// has been called throws <see cref="CryptographicUnexpectedOperationException" /> rather than
    /// silently mutating the digest length mid-computation.
    /// </summary>
    [TestMethod]
    public void HashSize_WhenSetAfterTransformBlock_ShouldThrowExactly()
    {
        using var algorithm = CreateAlgorithm();
        byte[] input = new byte[] { 0x01, 0x02, 0x03 };
        algorithm.TransformBlock(input, 0, input.Length, null, 0);

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            algorithm.HashSize = 128;
        });
    }

    /// <summary>
    /// Verifies that constructing a <see cref="Tiger" /> with an unsupported hash size throws
    /// <see cref="ArgumentOutOfRangeException" /> rather than allowing the bad size to silently
    /// propagate to <see cref="HashAlgorithm.HashSize" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(64)]
    [DataRow(96)]
    [DataRow(193)]
    [DataRow(256)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    [DataRow(int.MaxValue)]
    public void Ctor_WhenHashSizeIsInvalid_ShouldThrowExactly(int hashSize)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Tiger(hashSize);
        });
    }

    /// <summary>
    /// Verifies that <see cref="HashAlgorithm.ComputeHash(byte[])" /> on a disposed instance throws
    /// <see cref="ObjectDisposedException" /> rather than returning a stale or zeroed digest.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.ComputeHash(new byte[8]);
        });
    }

    /// <summary>
    /// Verifies that two consecutive <see cref="HashAlgorithm.ComputeHash(byte[])" /> calls
    /// against the same <see cref="Tiger" /> instance produce identical digests for identical
    /// input — guarding against state leakage across reuse.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInvokedRepeatedly_ShouldYieldIdenticalDigest()
    {
        using var algorithm = CreateAlgorithm();
        byte[] input = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

        byte[] first = algorithm.ComputeHash(input);
        byte[] second = algorithm.ComputeHash(input);

        CollectionAssert.AreEqual(first, second);
    }
}
