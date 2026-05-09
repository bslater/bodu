// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CubeHashTests.EdgeCases.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Probes <see cref="CubeHash" /> for unexpected exceptions when its public surface is exercised
/// outside of expected usage — mid-stream property mutation, non-multiple-of-8 hash sizes, and
/// idempotent disposal. The reflection-driven dispose tests in
/// <see cref="HashAlgorithmTests{TTest, TAlgorithm, TVariant}" /> already cover the trivial
/// disposed-state coverage; this file targets the algorithm-specific contract.
/// </summary>
public partial class CubeHashTests
{
    /// <summary>
    /// Verifies that assigning <see cref="CubeHash.HashSize" /> to a value that is not a positive
    /// multiple of 8 throws <see cref="ArgumentOutOfRangeException" /> rather than producing a
    /// digest of unexpected length.
    /// </summary>
    [TestMethod]
    [DataRow(9)]
    [DataRow(15)]
    [DataRow(17)]
    [DataRow(31)]
    [DataRow(33)]
    [DataRow(127)]
    [DataRow(129)]
    [DataRow(255)]
    [DataRow(511)]
    public void HashSize_WhenSetToNonMultipleOfEight_ShouldThrowExactly(int value)
    {
        using var algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.HashSize = value;
        });
    }

    /// <summary>
    /// Verifies that assigning <see cref="CubeHash.FinalizationRounds" /> after
    /// <see cref="HashAlgorithm.TransformBlock" /> has been called throws
    /// <see cref="CryptographicUnexpectedOperationException" /> rather than silently
    /// reconfiguring rounds mid-computation.
    /// </summary>
    [TestMethod]
    public void FinalizationRounds_WhenSetAfterTransformBlock_ShouldThrowExactly()
    {
        using var algorithm = CreateAlgorithm();
        byte[] input = new byte[] { 0x01, 0x02, 0x03 };
        algorithm.TransformBlock(input, 0, input.Length, null, 0);

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            algorithm.FinalizationRounds = 64;
        });
    }

    /// <summary>
    /// Verifies that assigning <see cref="CubeHash.InitializationRounds" /> after
    /// <see cref="HashAlgorithm.TransformBlock" /> has been called throws
    /// <see cref="CryptographicUnexpectedOperationException" />.
    /// </summary>
    [TestMethod]
    public void InitializationRounds_WhenSetAfterTransformBlock_ShouldThrowExactly()
    {
        using var algorithm = CreateAlgorithm();
        byte[] input = new byte[] { 0x01, 0x02, 0x03 };
        algorithm.TransformBlock(input, 0, input.Length, null, 0);

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            algorithm.InitializationRounds = 32;
        });
    }

    /// <summary>
    /// Verifies that assigning <see cref="CubeHash.TransformBlockSize" /> after
    /// <see cref="HashAlgorithm.TransformBlock" /> has been called throws
    /// <see cref="CryptographicUnexpectedOperationException" />.
    /// </summary>
    [TestMethod]
    public void TransformBlockSize_WhenSetAfterTransformBlock_ShouldThrowExactly()
    {
        using var algorithm = CreateAlgorithm();
        byte[] input = new byte[] { 0x01, 0x02, 0x03 };
        algorithm.TransformBlock(input, 0, input.Length, null, 0);

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            algorithm.TransformBlockSize = 64;
        });
    }

    /// <summary>
    /// Verifies that assigning <see cref="CubeHash.HashSize" /> after
    /// <see cref="HashAlgorithm.TransformBlock" /> has been called throws
    /// <see cref="CryptographicUnexpectedOperationException" />.
    /// </summary>
    [TestMethod]
    public void HashSize_WhenSetAfterTransformBlock_ShouldThrowExactly()
    {
        using var algorithm = CreateAlgorithm();
        byte[] input = new byte[] { 0x01, 0x02, 0x03 };
        algorithm.TransformBlock(input, 0, input.Length, null, 0);

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            algorithm.HashSize = 256;
        });
    }

    /// <summary>
    /// Verifies that <see cref="CubeHash.AlgorithmName" /> returns the documented format
    /// <c>CubeHash{r}+{b}/{w}+{f}-{h}</c> using the algorithm's current property values.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenReadAfterCustomConfiguration_ShouldFormatAllParameters()
    {
        using var algorithm = new CubeHash
        {
            InitializationRounds = 16,
            Rounds = 16,
            TransformBlockSize = 32,
            FinalizationRounds = 32,
            HashSize = 256,
        };

        Assert.AreEqual("CubeHash16+16/32+32-256", algorithm.AlgorithmName);
    }

    /// <summary>
    /// Verifies that calling <see cref="CubeHash.Dispose" /> twice on the same instance is
    /// idempotent and does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var algorithm = new CubeHash();
        algorithm.Dispose();

        try
        {
            algorithm.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Second Dispose on CubeHash threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that disposing a freshly-constructed <see cref="CubeHash" /> instance — one that
    /// has never had any property accessed or hashing performed — completes without throwing.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInstanceUntouched_ShouldNotThrow()
    {
        var algorithm = new CubeHash();

        try
        {
            algorithm.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Disposing an untouched CubeHash instance threw {ex.GetType().Name}: {ex.Message}");
        }
    }
}
