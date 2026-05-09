// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackAlgorithmTests.CreateDecryptor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class SkipjackAlgorithmTests
{
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
}
