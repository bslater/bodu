// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleBlockComputationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="MerkleBlockComputation" />, the result of a block-mode pass over
/// <see cref="MerkleTree" />.
/// </summary>
[TestClass]
public partial class MerkleBlockComputationTests
{
    /// <summary>The block size every test computes with; small, so a few bytes yield several blocks.</summary>
    private const int TestBlockSize = 4;

    /// <summary>
    /// Computes a block-mode result over a deterministic input of the specified length.
    /// </summary>
    /// <param name="inputLength">The number of input bytes.</param>
    /// <param name="blockSize">The block size to divide the input by.</param>
    /// <returns>The computation, with its input available through <see cref="Input(int)" />.</returns>
    private static MerkleBlockComputation Compute(int inputLength, int blockSize = TestBlockSize) =>
        new MerkleTree(SHA256.Create).ComputeBlocked(Input(inputLength), blockSize);

    /// <summary>
    /// Synthesizes the deterministic input <see cref="Compute(int, int)" /> hashes: byte <c>i</c> is <c>i</c> modulo
    /// 256.
    /// </summary>
    /// <param name="inputLength">The number of bytes.</param>
    /// <returns>The input bytes.</returns>
    private static byte[] Input(int inputLength)
    {
        var bytes = new byte[inputLength];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = unchecked((byte)i);

        return bytes;
    }
}
