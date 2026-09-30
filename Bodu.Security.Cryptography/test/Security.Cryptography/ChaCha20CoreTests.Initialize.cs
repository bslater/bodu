// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20CoreTests.Initialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class ChaCha20CoreTests
{
    /// <summary>
    /// Verifies that a key and a nonce seed the state RFC 8439 Section 2.3.2 lays out - the constant, the key words, and
    /// the nonce words, all little-endian - with a zero counter word.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenKeyAndNonceAreValid_ShouldLayOutTheRfc8439State()
    {
        byte[] key = Convert.FromHexString("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F");
        byte[] nonce = Convert.FromHexString("000000090000004A00000000");
        uint[] state = new uint[ChaCha20Core.StateWords];
        Array.Fill(state, 0xDEADBEEFU);

        ChaCha20Core.Initialize(state, key, nonce);

        uint[] expected =
        [
            0x61707865, 0x3320646e, 0x79622d32, 0x6b206574,
            0x03020100, 0x07060504, 0x0b0a0908, 0x0f0e0d0c,
            0x13121110, 0x17161514, 0x1b1a1918, 0x1f1e1d1c,
            0x00000000, 0x09000000, 0x4a000000, 0x00000000,
        ];
        CollectionAssert.AreEqual(expected, state);
    }

    /// <summary>
    /// Verifies that a key that is not 32 bytes long is rejected with <see cref="ArgumentOutOfRangeException" /> naming
    /// the parameter.
    /// </summary>
    /// <param name="length">The rejected key length.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(16)]
    [DataRow(31)]
    [DataRow(33)]
    public void Initialize_WhenKeyIsNot32Bytes_ShouldThrowArgumentOutOfRangeException(int length)
    {
        uint[] state = new uint[ChaCha20Core.StateWords];
        byte[] key = new byte[length];
        byte[] nonce = new byte[ChaCha20Core.NonceBytes];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ChaCha20Core.Initialize(state, key, nonce);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a nonce that is not 12 bytes long is rejected with <see cref="ArgumentOutOfRangeException" />
    /// naming the parameter.
    /// </summary>
    /// <param name="length">The rejected nonce length.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(8)]
    [DataRow(11)]
    [DataRow(16)]
    public void Initialize_WhenNonceIsNot12Bytes_ShouldThrowArgumentOutOfRangeException(int length)
    {
        uint[] state = new uint[ChaCha20Core.StateWords];
        byte[] key = new byte[ChaCha20Core.KeyBytes];
        byte[] nonce = new byte[length];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ChaCha20Core.Initialize(state, key, nonce);
        });

        Assert.AreEqual("nonce", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a state shorter than sixteen words is rejected with <see cref="ArgumentOutOfRangeException" />
    /// naming the parameter.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenStateHoldsFewerThanSixteenWords_ShouldThrowArgumentOutOfRangeException()
    {
        uint[] state = new uint[ChaCha20Core.StateWords - 1];
        byte[] key = new byte[ChaCha20Core.KeyBytes];
        byte[] nonce = new byte[ChaCha20Core.NonceBytes];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ChaCha20Core.Initialize(state, key, nonce);
        });

        Assert.AreEqual("state", ex.ParamName);
    }
}
