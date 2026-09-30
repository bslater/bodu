// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Blake2bTests.HashVariableLength.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.Security.Cryptography;

public partial class Argon2Blake2bTests
{
    /// <summary>
    /// Verifies that <c>H'</c> matches a direct transcription of RFC 9106, Figure 8 - built from the one-shot BLAKE2b -
    /// for output lengths on both sides of the 64-byte digest limit and at the chaining boundaries.
    /// </summary>
    /// <param name="outputLength">The requested output length <c>T</c>, in bytes.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(32)]
    [DataRow(64)]
    [DataRow(65)]
    [DataRow(96)]
    [DataRow(97)]
    [DataRow(128)]
    [DataRow(1024)]
    public void HashVariableLength_WhenGivenAnyOutputLength_ShouldMatchRfc9106Figure8(int outputLength)
    {
        byte[] input = CreateMessage(72);

        byte[] actual = new byte[outputLength];
        Argon2Blake2b.HashVariableLength(input, actual);

        CollectionAssert.AreEqual(Figure8(input, outputLength), actual);
    }

    /// <summary>
    /// Transcribes RFC 9106, Figure 8 over the one-shot BLAKE2b, allocating freely for clarity.
    /// </summary>
    /// <param name="input">The message <c>A</c>.</param>
    /// <param name="outputLength">The output length <c>T</c>.</param>
    /// <returns><c>H'^T(A)</c>.</returns>
    private static byte[] Figure8(byte[] input, int outputLength)
    {
        byte[] prefixed = new byte[4 + input.Length];
        BinaryPrimitives.WriteInt32LittleEndian(prefixed, outputLength);
        input.CopyTo(prefixed, 4);

        byte[] output = new byte[outputLength];
        if (outputLength <= 64)
        {
            Argon2Blake2b.Hash(prefixed, output);
            return output;
        }

        int r = ((outputLength + 31) / 32) - 2;
        byte[] v = new byte[64];
        Argon2Blake2b.Hash(prefixed, v);
        Array.Copy(v, 0, output, 0, 32);

        for (int i = 2; i <= r; i++)
        {
            byte[] next = new byte[64];
            Argon2Blake2b.Hash(v, next);
            v = next;
            Array.Copy(v, 0, output, (i - 1) * 32, 32);
        }

        byte[] last = new byte[outputLength - (32 * r)];
        Argon2Blake2b.Hash(v, last);
        last.CopyTo(output, 32 * r);
        return output;
    }
}
