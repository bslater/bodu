// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Blake2bTests.Hasher.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Argon2Blake2bTests
{
    /// <summary>
    /// Verifies that appending a message in chunks of any size produces the one-shot digest, for messages that end
    /// before, on, and after a 128-byte block boundary.
    /// </summary>
    /// <param name="messageLength">The message length, in bytes.</param>
    /// <param name="chunkLength">The length of each appended chunk, in bytes.</param>
    [TestMethod]
    [DataRow(0, 1)]
    [DataRow(1, 1)]
    [DataRow(127, 7)]
    [DataRow(128, 1)]
    [DataRow(128, 128)]
    [DataRow(129, 64)]
    [DataRow(255, 128)]
    [DataRow(256, 200)]
    [DataRow(257, 3)]
    [DataRow(1000, 129)]
    public void Hasher_WhenMessageIsAppendedInChunks_ShouldMatchOneShotDigest(int messageLength, int chunkLength)
    {
        byte[] message = CreateMessage(messageLength);
        byte[] expected = new byte[Argon2Blake2b.MaxDigestBytes];
        Argon2Blake2b.Hash(message, expected);

        Span<ulong> state = stackalloc ulong[Argon2Blake2b.StateWords];
        Span<byte> block = stackalloc byte[Argon2Blake2b.BlockSizeBytes];
        var hasher = new Argon2Blake2b.Hasher(state, block, Argon2Blake2b.MaxDigestBytes);
        for (int offset = 0; offset < message.Length; offset += chunkLength)
            hasher.Append(message.AsSpan(offset, Math.Min(chunkLength, message.Length - offset)));

        byte[] actual = new byte[Argon2Blake2b.MaxDigestBytes];
        hasher.Finish(actual);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that a 32-bit value appended in little-endian order hashes the same as its four bytes appended directly.
    /// </summary>
    [TestMethod]
    public void Hasher_WhenValueIsAppendedLittleEndian_ShouldMatchItsBytes()
    {
        byte[] expected = new byte[32];
        Argon2Blake2b.Hash([0x78, 0x56, 0x34, 0x12], expected);

        Span<ulong> state = stackalloc ulong[Argon2Blake2b.StateWords];
        Span<byte> block = stackalloc byte[Argon2Blake2b.BlockSizeBytes];
        var hasher = new Argon2Blake2b.Hasher(state, block, 32);
        hasher.AppendLittleEndian(0x12345678);

        byte[] actual = new byte[32];
        hasher.Finish(actual);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that finishing a digest clears the caller-supplied state and block buffer, which held the message.
    /// </summary>
    [TestMethod]
    public void Hasher_WhenFinished_ShouldClearStateAndBlock()
    {
        ulong[] state = new ulong[Argon2Blake2b.StateWords];
        byte[] block = new byte[Argon2Blake2b.BlockSizeBytes];
        var hasher = new Argon2Blake2b.Hasher(state, block, Argon2Blake2b.MaxDigestBytes);
        hasher.Append(CreateMessage(300));

        hasher.Finish(new byte[Argon2Blake2b.MaxDigestBytes]);

        Assert.IsTrue(Array.TrueForAll(state, word => word == 0), "The chaining state must be cleared.");
        Assert.IsTrue(Array.TrueForAll(block, value => value == 0), "The block buffer must be cleared.");
    }

    /// <summary>
    /// Creates a deterministic message of the specified length.
    /// </summary>
    /// <param name="length">The message length, in bytes.</param>
    /// <returns>The message.</returns>
    private static byte[] CreateMessage(int length)
    {
        byte[] message = new byte[length];
        for (int i = 0; i < length; i++)
            message[i] = (byte)((i * 31) + 7);

        return message;
    }
}
