// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish256TweakableAlgorithmTests.CreateEncryptor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class Threefish256TweakableAlgorithmTests
{
    /// <summary>
    /// Verifies that <see cref="Threefish.CreateEncryptor(byte[], byte[], byte[])" /> with a
    /// wrong-sized key throws <see cref="CryptographicException" />. Values are specific to
    /// Threefish-256's 256-bit (32-byte) key size.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(16)]
    [DataRow(31)]
    [DataRow(33)]
    [DataRow(64)]
    public void CreateEncryptor_WhenKeyLengthIsInvalid_ShouldThrowCryptographicException(int keyLength)
    {
        using Threefish256 algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.CreateEncryptor(new byte[keyLength], new byte[32], new byte[16]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Threefish.CreateEncryptor(byte[], byte[], byte[])" /> with a
    /// wrong-sized IV throws <see cref="CryptographicException" />. Values are specific to
    /// Threefish-256's 256-bit (32-byte) block size.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(16)]
    [DataRow(31)]
    [DataRow(33)]
    [DataRow(64)]
    public void CreateEncryptor_WhenIVLengthIsInvalid_ShouldThrowCryptographicException(int ivLength)
    {
        using Threefish256 algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.CreateEncryptor(new byte[32], new byte[ivLength], new byte[16]);
        });
    }
}
