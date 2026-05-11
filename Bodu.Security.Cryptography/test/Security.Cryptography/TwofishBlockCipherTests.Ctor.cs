// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TwofishBlockCipherTests.Ctor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal sealed partial class TwofishBlockCipherTests
{
    /// <summary>
    /// Verifies that constructing <see cref="TwofishBlockCipher" /> with a key length that is not 16, 24,
    /// or 32 bytes throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(15)]
    [DataRow(17)]
    [DataRow(23)]
    [DataRow(25)]
    [DataRow(31)]
    [DataRow(33)]
    public void Ctor_WhenKeyLengthIsInvalid_ShouldThrowArgumentException(int keyLength)
    {
        byte[] key = new byte[keyLength];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            using var _ = new TwofishBlockCipher(key);
        });
    }

    /// <summary>
    /// Verifies that constructing <see cref="TwofishBlockCipher" /> with each valid key length (16, 24,
    /// and 32 bytes) succeeds without throwing.
    /// </summary>
    [TestMethod]
    [DataRow(16)]
    [DataRow(24)]
    [DataRow(32)]
    public void Ctor_WhenKeyLengthIsValid_ShouldNotThrow(int keyLength)
    {
        byte[] key = new byte[keyLength];

        using var cipher = new TwofishBlockCipher(key);
        Assert.IsNotNull(cipher);
    }
}
