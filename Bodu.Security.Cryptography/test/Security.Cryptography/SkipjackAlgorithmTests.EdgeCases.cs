// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackAlgorithmTests.EdgeCases.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Probes <see cref="Skipjack" /> for unexpected exceptions when its public surface is
/// exercised outside of expected usage — wrong-sized key/IV material, idempotent
/// disposal, untouched-instance disposal, and the algorithm-specific
/// <see cref="Skipjack.BlockMode" /> property.
/// </summary>
public sealed partial class SkipjackAlgorithmTests
{
    /// <summary>
    /// Verifies that <see cref="Skipjack.CreateEncryptor(byte[], byte[])" /> with an out-of-range
    /// key length throws <see cref="CryptographicException" /> for every wrong size.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(9)]
    [DataRow(11)]
    [DataRow(16)]
    [DataRow(32)]
    public void CreateEncryptor_WhenKeyLengthIsInvalid_ShouldThrowExactly(int keyLength)
    {
        using var algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.CreateEncryptor(new byte[keyLength], new byte[8]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Skipjack.CreateEncryptor(byte[], byte[])" /> with an out-of-range
    /// IV length throws <see cref="CryptographicException" /> for every wrong size.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(9)]
    [DataRow(16)]
    public void CreateEncryptor_WhenIVLengthIsInvalid_ShouldThrowExactly(int ivLength)
    {
        using var algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.CreateEncryptor(new byte[10], new byte[ivLength]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Skipjack.CreateDecryptor(byte[], byte[])" /> with an out-of-range
    /// key length throws <see cref="CryptographicException" /> for every wrong size.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(9)]
    [DataRow(11)]
    [DataRow(16)]
    [DataRow(32)]
    public void CreateDecryptor_WhenKeyLengthIsInvalid_ShouldThrowExactly(int keyLength)
    {
        using var algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.CreateDecryptor(new byte[keyLength], new byte[8]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Skipjack.CreateDecryptor(byte[], byte[])" /> with an out-of-range
    /// IV length throws <see cref="CryptographicException" /> for every wrong size.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(9)]
    [DataRow(16)]
    public void CreateDecryptor_WhenIVLengthIsInvalid_ShouldThrowExactly(int ivLength)
    {
        using var algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.CreateDecryptor(new byte[10], new byte[ivLength]);
        });
    }

    /// <summary>
    /// Verifies that disposing a freshly-constructed <see cref="Skipjack" /> instance — one that has
    /// never had its key, IV, or any other property accessed — completes without throwing.
    /// Regression guard for disposal paths that touch lazily-initialised state without null checks.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInstanceUntouched_ShouldNotThrow()
    {
        var algorithm = CreateAlgorithm();

        try
        {
            algorithm.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Disposing an untouched Skipjack instance threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that calling <see cref="Skipjack.Dispose" /> twice is idempotent and does not throw.
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
            Assert.Fail($"Second Dispose on Skipjack threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that the algorithm-specific <see cref="Skipjack.BlockMode" /> property accepts
    /// every <see cref="CipherBlockMode" /> value without throwing — it is a plain auto-property
    /// and must not gain accidental enum validation.
    /// </summary>
    [TestMethod]
    [DataRow(CipherBlockMode.ECB)]
    [DataRow(CipherBlockMode.CBC)]
    [DataRow(CipherBlockMode.CFB)]
    [DataRow(CipherBlockMode.OFB)]
    [DataRow(CipherBlockMode.CTR)]
    public void BlockMode_WhenSetToValidValue_ShouldNotThrow(CipherBlockMode mode)
    {
        using var algorithm = CreateAlgorithm();

        try
        {
            algorithm.BlockMode = mode;
        }
        catch (Exception ex)
        {
            Assert.Fail($"Setting Skipjack.BlockMode = {mode} threw {ex.GetType().Name}: {ex.Message}");
        }
    }
}
