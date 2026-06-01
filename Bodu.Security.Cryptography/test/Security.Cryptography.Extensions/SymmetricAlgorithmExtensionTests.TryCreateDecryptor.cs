// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmExtensionTests.TryCreateDecryptor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Tests for the <see cref="SymmetricAlgorithmExtensions.TryCreateDecryptor(SymmetricAlgorithm, out ICryptoTransform)" />
/// family — covering the parameterless overload and the explicit key/IV overload.
/// </summary>
/// <remarks>
/// The try-pattern returns <see langword="false" /> with a <see langword="null" /> out
/// parameter for any input that would cause the underlying
/// <see cref="SymmetricAlgorithm.CreateDecryptor(byte[], byte[])" /> to throw — null arguments,
/// invalid key sizes, or invalid IV sizes.
/// </remarks>
public partial class SymmetricAlgorithmExtensionTests
{
    // ─── Success paths ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the parameterless overload auto-generates a key and IV and returns a
    /// non-null transform.
    /// </summary>
    [TestMethod]
    public void TryCreateDecryptor_WhenKeyAndIvAreUnset_ShouldGenerateAndReturnTransform()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();

        var result = algorithm.TryCreateDecryptor(out ICryptoTransform? transform);

        Assert.IsTrue(result);
        Assert.IsNotNull(transform);
    }

    /// <summary>
    /// Verifies that the key/IV overload returns <see langword="true" /> and a non-null
    /// transform for valid zero-filled key and IV arrays of the correct lengths.
    /// </summary>
    [TestMethod]
    public void TryCreateDecryptor_WhenValid_ShouldReturnTrue()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();

        var key = new byte[algorithm.KeySize / 8];
        var iv = new byte[algorithm.BlockSize / 8];

        var result = algorithm.TryCreateDecryptor(key, iv, out ICryptoTransform? transform);

        Assert.IsTrue(result);
        Assert.IsNotNull(transform);
    }

    // ─── Failure paths — null arguments ───────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a null key returns <see langword="false" /> with a null out parameter.
    /// </summary>
    [TestMethod]
    public void TryCreateDecryptor_WhenKeyIsNull_ShouldReturnFalseAndNullOutput()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();

        var result = algorithm.TryCreateDecryptor(null!, new byte[algorithm.BlockSize / 8], out ICryptoTransform? transform);

        Assert.IsFalse(result);
        Assert.IsNull(transform);
    }

    /// <summary>
    /// Verifies that a null IV returns <see langword="false" /> with a null out parameter.
    /// </summary>
    [TestMethod]
    public void TryCreateDecryptor_WhenIVIsNull_ShouldReturnFalseAndNullOutput()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();

        var result = algorithm.TryCreateDecryptor(new byte[algorithm.KeySize / 8], null!, out ICryptoTransform? transform);

        Assert.IsFalse(result);
        Assert.IsNull(transform);
    }

    // ─── Failure paths — invalid sizes ────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that an IV with an invalid length (one byte too long) returns
    /// <see langword="false" /> with a null out parameter.
    /// </summary>
    [TestMethod]
    public void TryCreateDecryptor_WhenIVSizeIsInvalid_ShouldReturnFalseAndNullOutput()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();

        var key = new byte[algorithm.KeySize / 8];
        var iv = new byte[(algorithm.BlockSize / 8) + 1]; // one byte too long

        var result = algorithm.TryCreateDecryptor(key, iv, out ICryptoTransform? transform);

        Assert.IsFalse(result);
        Assert.IsNull(transform);
    }

    /// <summary>
    /// Verifies that a key outside the algorithm's legal key sizes returns
    /// <see langword="false" /> with a null out parameter.
    /// </summary>
    /// <remarks>
    /// If the concrete algorithm accepts every byte-aligned key length (uncommon), no invalid
    /// size can be constructed and the test is marked inconclusive rather than failing.
    /// </remarks>
    [TestMethod]
    public void TryCreateDecryptor_WhenKeySizeIsInvalid_ShouldReturnFalseAndNullOutput()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();

        var invalidKey = CryptoTestUtilities.FindInvalidKeySize(algorithm.LegalKeySizes);

        if (invalidKey is null)
        {
            Assert.Inconclusive(
                $"{algorithm.GetType().Name} accepts every byte-aligned key length — " +
                "no invalid size can be constructed for this test.");
            return;
        }

        var iv = new byte[algorithm.BlockSize / 8];

        var result = algorithm.TryCreateDecryptor(invalidKey, iv, out ICryptoTransform? transform);

        Assert.IsFalse(result,
            $"TryCreateDecryptor must return false for a {invalidKey.Length * 8}-bit key " +
            $"that is outside the legal key sizes for {algorithm.GetType().Name}.");
        Assert.IsNull(transform,
            "Output transform must be null when TryCreateDecryptor returns false.");
    }
}
