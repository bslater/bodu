// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Salsa20CoreTests.Block.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Salsa20CoreTests
{
    /// <summary>
    /// Verifies that the core function reproduces the first keystream block of published Salsa20 vectors: two eSTREAM
    /// vectors under 256-bit keys and an ECRYPT vector under a 128-bit key.
    /// </summary>
    /// <param name="testName">The vector's name.</param>
    /// <param name="key">The key, in hexadecimal.</param>
    /// <param name="nonce">The nonce, in hexadecimal.</param>
    /// <param name="keystream">The first 64 bytes of keystream, in hexadecimal.</param>
    [TestMethod]
    [DataRow(
        "eSTREAM Salsa20/256 KEY1 IV0",
        "8000000000000000000000000000000000000000000000000000000000000000",
        "0000000000000000",
        "E3BE8FDD8BECA2E3EA8EF9475B29A6E7003951E1097A5C38D23B7A5FAD9F6844B22C97559E2723C7CBBD3FE4FC8D9A0744652A83E72A9C461876AF4D7EF1A117")]
    [DataRow(
        "eSTREAM Salsa20/256 KEY0 IV1",
        "0000000000000000000000000000000000000000000000000000000000000000",
        "8000000000000000",
        "2ABA3DC45B4947007B14C851CD694456B303AD59A465662803006705673D6C3E29F1D3510DFC0405463C03414E0E07E359F1F1816C68B2434A19D3EEE0464873")]
    [DataRow(
        "ECRYPT Salsa20/128 Set 1 vector 0",
        "80000000000000000000000000000000",
        "0000000000000000",
        "4DFA5E481DA23EA09A31022050859936DA52FCEE218005164F267CB65F5CFD7F2B4F97E0FF16924A52DF269515110A07F9E460BC65EF95DA58F740B7D1DBB0AA")]
    public void Block_WithPublishedVector_ShouldProduceFirstKeystreamBlock(string testName, string key, string nonce, string keystream)
    {
        uint[] state = new uint[Salsa20Core.StateWords];
        Salsa20Core.Initialize(state, Convert.FromHexString(key), Convert.FromHexString(nonce));
        byte[] block = new byte[Salsa20Core.BlockBytes];

        Salsa20Core.Block(state, 0, block);

        CollectionAssert.AreEqual(Convert.FromHexString(keystream), block, testName);
    }

    /// <summary>
    /// Verifies that the core function takes its counter from the argument, both halves of it, whatever the state's
    /// counter words hold.
    /// </summary>
    [TestMethod]
    public void Block_WhenStateCounterWordsDiffer_ShouldUseTheCounterArgument()
    {
        var random = new Random(0x5A15_0001);
        uint[] state = NextState(random);
        uint[] other = (uint[])state.Clone();
        other[Salsa20Core.CounterWord] ^= 0x8000_0001;
        other[Salsa20Core.CounterWord + 1] ^= 0x0000_0100;
        byte[] expected = new byte[Salsa20Core.BlockBytes];
        byte[] actual = new byte[Salsa20Core.BlockBytes];

        Salsa20Core.Block(state, 0x0000_0001_0000_0007UL, expected);
        Salsa20Core.Block(other, 0x0000_0001_0000_0007UL, actual);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that a destination shorter than a block is rejected with <see cref="ArgumentOutOfRangeException" />
    /// naming the parameter.
    /// </summary>
    [TestMethod]
    public void Block_WhenDestinationIsShorterThanABlock_ShouldThrowArgumentOutOfRangeException()
    {
        uint[] state = new uint[Salsa20Core.StateWords];
        byte[] destination = new byte[Salsa20Core.BlockBytes - 1];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Salsa20Core.Block(state, 0, destination);
        });

        Assert.AreEqual("destination", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a state shorter than sixteen words is rejected with <see cref="ArgumentOutOfRangeException" />
    /// naming the parameter.
    /// </summary>
    [TestMethod]
    public void Block_WhenStateHoldsFewerThanSixteenWords_ShouldThrowArgumentOutOfRangeException()
    {
        uint[] state = new uint[Salsa20Core.StateWords - 1];
        byte[] destination = new byte[Salsa20Core.BlockBytes];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Salsa20Core.Block(state, 0, destination);
        });

        Assert.AreEqual("state", ex.ParamName);
    }
}
