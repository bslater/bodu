// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Salsa20CoreTests.Initialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Salsa20CoreTests
{
    /// <summary>
    /// Verifies that a 256-bit key and a nonce seed the state the specification lays out: the constant on the diagonal,
    /// the key halves in words 1-4 and 11-14, the nonce in words 6 and 7, and a zero counter in words 8 and 9.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenKeyIs256Bits_ShouldLayOutTheState()
    {
        byte[] key = Convert.FromHexString("000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F");
        byte[] nonce = Convert.FromHexString("2021222324252627");
        uint[] state = new uint[Salsa20Core.StateWords];
        Array.Fill(state, 0xDEADBEEFU);

        Salsa20Core.Initialize(state, key, nonce);

        uint[] expected =
        [
            0x61707865, 0x03020100, 0x07060504, 0x0b0a0908,
            0x0f0e0d0c, 0x3320646e, 0x23222120, 0x27262524,
            0x00000000, 0x00000000, 0x79622d32, 0x13121110,
            0x17161514, 0x1b1a1918, 0x1f1e1d1c, 0x6b206574,
        ];
        CollectionAssert.AreEqual(expected, state);
    }

    /// <summary>
    /// Verifies that a 128-bit key fills both key halves and takes the <c>"expand 16-byte k"</c> constant.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenKeyIs128Bits_ShouldRepeatTheKeyUnderTheTauConstant()
    {
        byte[] key = Convert.FromHexString("000102030405060708090A0B0C0D0E0F");
        byte[] nonce = Convert.FromHexString("2021222324252627");
        uint[] state = new uint[Salsa20Core.StateWords];

        Salsa20Core.Initialize(state, key, nonce);

        uint[] expected =
        [
            0x61707865, 0x03020100, 0x07060504, 0x0b0a0908,
            0x0f0e0d0c, 0x3120646e, 0x23222120, 0x27262524,
            0x00000000, 0x00000000, 0x79622d36, 0x03020100,
            0x07060504, 0x0b0a0908, 0x0f0e0d0c, 0x6b206574,
        ];
        CollectionAssert.AreEqual(expected, state);
    }

    /// <summary>
    /// Verifies that a key neither 16 nor 32 bytes long is rejected with <see cref="ArgumentOutOfRangeException" />
    /// naming the parameter.
    /// </summary>
    /// <param name="length">The rejected key length.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(15)]
    [DataRow(24)]
    [DataRow(33)]
    public void Initialize_WhenKeyIsNeither16Nor32Bytes_ShouldThrowArgumentOutOfRangeException(int length)
    {
        uint[] state = new uint[Salsa20Core.StateWords];
        byte[] key = new byte[length];
        byte[] nonce = new byte[Salsa20Core.NonceBytes];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Salsa20Core.Initialize(state, key, nonce);
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a nonce that is not 8 bytes long is rejected with <see cref="ArgumentOutOfRangeException" />
    /// naming the parameter.
    /// </summary>
    /// <param name="length">The rejected nonce length.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(7)]
    [DataRow(12)]
    public void Initialize_WhenNonceIsNot8Bytes_ShouldThrowArgumentOutOfRangeException(int length)
    {
        uint[] state = new uint[Salsa20Core.StateWords];
        byte[] key = new byte[Salsa20Core.Key256Bytes];
        byte[] nonce = new byte[length];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Salsa20Core.Initialize(state, key, nonce);
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
        uint[] state = new uint[Salsa20Core.StateWords - 1];
        byte[] key = new byte[Salsa20Core.Key256Bytes];
        byte[] nonce = new byte[Salsa20Core.NonceBytes];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Salsa20Core.Initialize(state, key, nonce);
        });

        Assert.AreEqual("state", ex.ParamName);
    }
}
