// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishAlgorithmTests.CreateEncryptor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class ThreefishAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that <see cref="Threefish.CreateEncryptor(byte[], byte[], byte[])" /> with a
    /// wrong-sized tweak throws <see cref="CryptographicException" /> rather than producing a
    /// transform with truncated or padded tweak material. All Threefish variants use a 128-bit
    /// (16-byte) tweak, so these values are universally invalid.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(8)]
    [DataRow(15)]
    [DataRow(17)]
    [DataRow(32)]
    public void CreateEncryptor_WhenTweakLengthIsInvalid_ShouldThrowCryptographicException(int tweakLength)
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.CreateEncryptor(new byte[algorithm.KeySize / 8], new byte[algorithm.BlockSize / 8], new byte[tweakLength]);
        });
    }
}
