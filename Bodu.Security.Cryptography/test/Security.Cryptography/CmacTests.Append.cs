// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CmacTests.Append.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class CmacTests
{
    /// <summary>
    /// Verifies that the MAC matches a block-at-a-time reference at lengths on both sides of every boundary the
    /// incremental CMAC handles - a block, the chained-call threshold, and a 4 KiB chunk - for a cipher that chains
    /// natively and one that does not.
    /// </summary>
    [TestMethod]
    public void Append_WhenMessageIsAppendedWhole_ShouldMatchBlockAtATimeReference()
    {
        byte[] key = AesReference.RandomBytes(16, 61);
        using System.Security.Cryptography.Aes reference = AesReference.Create(key);
        using var chained = new AesBlockCipher(key);
        using var unchained = new AesBlockCipherFixture(key);

        foreach (int length in new[] { 0, 1, 15, 16, 17, 32, 80, 95, 96, 97, 112, 4095, 4096, 4097, 20000 })
        {
            byte[] message = AesReference.RandomBytes(length, length + 62);
            byte[] expected = AesReference.Cmac(reference, message);

            CollectionAssert.AreEqual(expected, ComputeInPieces(chained, message, length), $"chained, {length} bytes");
            CollectionAssert.AreEqual(expected, ComputeInPieces(unchained, message, length), $"unchained, {length} bytes");
        }
    }

    /// <summary>
    /// Verifies that appending a message in pieces - empty ones, pieces that end on and off block boundaries, and a
    /// last piece that completes a block - gives the same MAC as appending it whole.
    /// </summary>
    [TestMethod]
    public void Append_WhenMessageIsAppendedInPieces_ShouldMatchAppendingItWhole()
    {
        byte[] key = AesReference.RandomBytes(16, 63);
        using var cipher = new AesBlockCipher(key);
        byte[] message = AesReference.RandomBytes(5000, 64);
        byte[] whole = ComputeInPieces(cipher, message, message.Length);

        int[][] shapes =
        [
            [0, 5000],
            [16, 4984],
            [1, 15, 16, 4968],
            [17, 0, 4983],
            [4096, 904],
            [4999, 1],
            [100, 100, 100, 4700],
            [16, 16, 16, 16, 4936],
        ];

        foreach (int[] shape in shapes)
            CollectionAssert.AreEqual(whole, ComputeInPieces(cipher, message, shape), string.Join(", ", shape));
    }
}
