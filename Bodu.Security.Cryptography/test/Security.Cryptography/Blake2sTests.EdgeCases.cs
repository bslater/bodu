// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2sTests.EdgeCases.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Probes <see cref="Blake2s" /> for unexpected exceptions when its public surface is exercised
/// outside of expected usage — invalid hash-size constructor arguments, mid-stream
/// <see cref="Blake2s.HashSize" /> mutation, and idempotent disposal.
/// </summary>
public partial class Blake2sTests
{
    /// <summary>
    /// Verifies that constructing a <see cref="Blake2s" /> with an unsupported hash size throws
    /// <see cref="ArgumentOutOfRangeException" /> rather than allowing the bad size to silently
    /// propagate to <see cref="HashAlgorithm.HashSize" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(127)]
    [DataRow(129)]
    [DataRow(257)]
    [DataRow(384)]
    [DataRow(512)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    [DataRow(int.MaxValue)]
    public void Ctor_WhenHashSizeIsInvalid_ShouldThrowExactly(int hashSize)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Blake2s(hashSize);
        });
    }

    /// <summary>
    /// Verifies that constructing a <see cref="Blake2s" /> with each documented valid hash size
    /// succeeds and reports the corresponding <see cref="HashAlgorithm.HashSize" /> value.
    /// </summary>
    [TestMethod]
    [DataRow(128)]
    [DataRow(160)]
    [DataRow(192)]
    [DataRow(224)]
    [DataRow(256)]
    public void Ctor_WhenHashSizeIsValid_ShouldSetHashSize(int hashSize)
    {
        using var algorithm = new Blake2s(hashSize);

        Assert.AreEqual(hashSize, algorithm.HashSize);
    }

    /// <summary>
    /// Verifies that assigning <see cref="Blake2s.HashSize" /> after
    /// <see cref="HashAlgorithm.TransformBlock" /> has been called throws
    /// <see cref="CryptographicUnexpectedOperationException" /> rather than silently mutating
    /// the digest length mid-computation.
    /// </summary>
    [TestMethod]
    public void HashSize_WhenSetAfterTransformBlock_ShouldThrowExactly()
    {
        using var algorithm = new Blake2s();
        byte[] input = new byte[] { 0x01, 0x02, 0x03 };
        algorithm.TransformBlock(input, 0, input.Length, null, 0);

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            algorithm.HashSize = 128;
        });
    }

    /// <summary>
    /// Verifies that assigning <see cref="Blake2s.HashSize" /> to an unsupported value throws
    /// <see cref="ArgumentOutOfRangeException" /> with no side effects on the existing
    /// <see cref="HashAlgorithm.HashSize" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(127)]
    [DataRow(129)]
    [DataRow(257)]
    [DataRow(384)]
    [DataRow(512)]
    [DataRow(-1)]
    public void HashSize_WhenSetToInvalidValue_ShouldThrowExactly(int value)
    {
        using var algorithm = new Blake2s();
        int originalSize = algorithm.HashSize;

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.HashSize = value;
        });

        Assert.AreEqual(originalSize, algorithm.HashSize,
            "Failed assignment must not mutate HashSize.");
    }

    /// <summary>
    /// Verifies that calling <see cref="Blake2s.Dispose" /> twice on the same instance is
    /// idempotent and does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var algorithm = new Blake2s();
        algorithm.Dispose();

        try
        {
            algorithm.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Second Dispose on Blake2s threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that disposing a freshly-constructed <see cref="Blake2s" /> instance — one that
    /// has never had its key, hash size, or any other property accessed — completes without
    /// throwing.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInstanceUntouched_ShouldNotThrow()
    {
        var algorithm = new Blake2s();

        try
        {
            algorithm.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Disposing an untouched Blake2s instance threw {ex.GetType().Name}: {ex.Message}");
        }
    }
}
