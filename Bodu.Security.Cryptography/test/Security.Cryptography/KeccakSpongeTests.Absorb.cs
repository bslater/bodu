// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeccakSpongeTests.Absorb.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class KeccakSpongeTests
{
    /// <summary>
    /// Builds the deterministic reference message used by the chunked absorb/squeeze equivalence tests. Long enough to
    /// cross both SHAKE rate boundaries (136 and 168 bytes) several times.
    /// </summary>
    /// <returns>The reference message.</returns>
    private static byte[] BuildSpongeReferenceMessage()
    {
        var message = new byte[521];
        for (int i = 0; i < message.Length; i++)
            message[i] = (byte)((i * 89) + 3);

        return message;
    }

    /// <summary>
    /// Verifies that absorbing the reference message in adversarial chunk sizes — spanning lane and rate boundaries —
    /// produces exactly the SHAKE128 output of a one-shot absorb.
    /// </summary>
    /// <param name="chunkSize">The chunk size to split the message into.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(9)]
    [DataRow(167)]
    [DataRow(168)]
    [DataRow(169)]
    public void Absorb_WhenInputSplitIntoChunks_ShouldMatchOneShotOutput(int chunkSize)
    {
        byte[] message = BuildSpongeReferenceMessage();

        KeccakSponge oneShot = KeccakSponge.CreateShake128();
        oneShot.Absorb(message);
        Span<byte> expected = stackalloc byte[200];
        oneShot.Squeeze(expected);
        oneShot.Clear();

        KeccakSponge chunked = KeccakSponge.CreateShake128();
        for (int offset = 0; offset < message.Length; offset += chunkSize)
            chunked.Absorb(message.AsSpan(offset, Math.Min(chunkSize, message.Length - offset)));
        Span<byte> actual = stackalloc byte[200];
        chunked.Squeeze(actual);
        chunked.Clear();

        CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray(),
            $"Chunked absorb (size {chunkSize}) diverged from the one-shot output.");
    }

    /// <summary>
    /// Verifies that squeezing the output stream in adversarial chunk sizes — spanning lane and rate boundaries —
    /// produces exactly the bytes of a single one-shot squeeze over the same absorbed input.
    /// </summary>
    /// <param name="chunkSize">The chunk size to squeeze at a time.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(8)]
    [DataRow(9)]
    [DataRow(135)]
    [DataRow(136)]
    [DataRow(137)]
    public void Squeeze_WhenOutputDrawnInChunks_ShouldMatchOneShotOutput(int chunkSize)
    {
        byte[] message = BuildSpongeReferenceMessage();

        KeccakSponge oneShot = KeccakSponge.CreateShake256();
        oneShot.Absorb(message);
        Span<byte> expected = stackalloc byte[413];
        oneShot.Squeeze(expected);
        oneShot.Clear();

        KeccakSponge chunked = KeccakSponge.CreateShake256();
        chunked.Absorb(message);
        Span<byte> actual = stackalloc byte[413];
        for (int offset = 0; offset < actual.Length; offset += chunkSize)
            chunked.Squeeze(actual.Slice(offset, Math.Min(chunkSize, actual.Length - offset)));
        chunked.Clear();

        CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray(),
            $"Chunked squeeze (size {chunkSize}) diverged from the one-shot output.");
    }
}
