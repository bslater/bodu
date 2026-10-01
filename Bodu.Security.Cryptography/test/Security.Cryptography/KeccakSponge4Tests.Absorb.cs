// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeccakSponge4Tests.Absorb.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class KeccakSponge4Tests
{
    /// <summary>
    /// Verifies that, for every message length from 0 to one byte short of the rate, each sponge squeezes the block a
    /// scalar sponge squeezes for its message, through each kernel and both SHAKE functions.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <param name="rate">The rate: 168 for SHAKE128, 136 for SHAKE256.</param>
    [TestMethod]
    [DataRow("Scalar", 168)]
    [DataRow("Scalar", 136)]
    [DataRow("Avx2", 168)]
    [DataRow("Avx2", 136)]
    [DataRow("Avx512", 168)]
    [DataRow("Avx512", 136)]
    public void Absorb_WhenMessagesAreEachLengthBelowTheRate_ForEachKernel_ShouldMatchFourScalarSponges(string kernel, int rate)
    {
        KeccakPermutation.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x4A_0000 + rate);

        for (int length = 0; length < rate; length++)
        {
            byte[][] messages = Messages(random, length);
            byte[][] blocks = Enumerable.Range(0, KeccakSponge4.Ways).Select(_ => new byte[rate]).ToArray();

            KeccakSponge4 sponge = Create(rate, kind);
            sponge.Absorb(messages[0], messages[1], messages[2], messages[3]);
            sponge.Squeeze(blocks[0], blocks[1], blocks[2], blocks[3]);

            for (int lane = 0; lane < KeccakSponge4.Ways; lane++)
                CollectionAssert.AreEqual(ScalarStream(rate, messages[lane], rate), blocks[lane], $"length {length}, sponge {lane}");
        }
    }

    /// <summary>
    /// Verifies that messages the same for every sponge give four identical streams, the scalar sponge's.
    /// </summary>
    [TestMethod]
    public void Absorb_WhenMessagesAreEqual_ShouldGiveFourEqualStreams()
    {
        byte[] message = [1, 2, 3, 4, 5];
        byte[][] blocks = Enumerable.Range(0, KeccakSponge4.Ways).Select(_ => new byte[KeccakSponge.Shake256RateBytes]).ToArray();

        KeccakSponge4 sponge = KeccakSponge4.CreateShake256();
        sponge.Absorb(message, message, message, message);
        sponge.Squeeze(blocks[0], blocks[1], blocks[2], blocks[3]);

        byte[] expected = ScalarStream(KeccakSponge.Shake256RateBytes, message, KeccakSponge.Shake256RateBytes);
        foreach (byte[] block in blocks)
            CollectionAssert.AreEqual(expected, block);
    }

    /// <summary>
    /// Verifies that a message as long as the rate, which one block cannot hold with its padding, is rejected with
    /// <see cref="ArgumentOutOfRangeException" /> naming the first message.
    /// </summary>
    /// <param name="rate">The rate: 168 for SHAKE128, 136 for SHAKE256.</param>
    [TestMethod]
    [DataRow(168)]
    [DataRow(136)]
    public void Absorb_WhenMessageIsNotShorterThanTheRate_ShouldThrowArgumentOutOfRangeException(int rate)
    {
        byte[] message = new byte[rate];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            KeccakSponge4 sponge = Create(rate, KeccakPermutation.KernelKind.Scalar);
            sponge.Absorb(message, message, message, message);
        });

        Assert.AreEqual("message0", ex.ParamName);
    }

    /// <summary>
    /// Verifies that messages of different lengths are rejected with <see cref="ArgumentException" /> naming the first
    /// that differs from the first message.
    /// </summary>
    /// <param name="shortMessage">The position of the message one byte shorter than the others: 1, 2 or 3.</param>
    /// <param name="expectedParamName">The name of the parameter that message is passed as.</param>
    [TestMethod]
    [DataRow(1, "message1")]
    [DataRow(2, "message2")]
    [DataRow(3, "message3")]
    public void Absorb_WhenMessagesDifferInLength_ShouldThrowArgumentException(int shortMessage, string expectedParamName)
    {
        byte[][] messages = Enumerable.Range(0, KeccakSponge4.Ways).Select(lane => new byte[lane == shortMessage ? 31 : 32]).ToArray();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            KeccakSponge4 sponge = KeccakSponge4.CreateShake128(KeccakPermutation.KernelKind.Scalar);
            sponge.Absorb(messages[0], messages[1], messages[2], messages[3]);
        });

        Assert.AreEqual(expectedParamName, ex.ParamName);
    }
}
