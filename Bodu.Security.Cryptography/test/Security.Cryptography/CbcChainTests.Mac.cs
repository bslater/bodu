// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CbcChainTests.Mac.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class CbcChainTests
{
    /// <summary>
    /// Verifies that the CBC-MAC state becomes the last block of the platform's CBC encryption from it, for a cipher that
    /// chains natively and one that does not, without an output buffer.
    /// </summary>
    [TestMethod]
    public void Mac_WhenGivenWholeBlocks_ShouldLeaveTheLastCiphertextBlockInState()
    {
        byte[] key = AesReference.RandomBytes(16, 81);
        using Aes platform = AesReference.Create(key);
        using var chained = new AesBlockCipher(key);
        using var unchained = new AesBlockCipherFixture(key);

        foreach (int length in s_lengths)
        {
            byte[] state = AesReference.RandomBytes(16, length + 82);
            byte[] blocks = AesReference.RandomBytes(length, length + 83);
            byte[] expected = platform.EncryptCbc(blocks, state, PaddingMode.None)[^16..];

            byte[] chainedState = (byte[])state.Clone();
            CbcChain.Mac(chained, blocks, chainedState);
            byte[] unchainedState = (byte[])state.Clone();
            CbcChain.Mac(unchained, blocks, unchainedState);

            CollectionAssert.AreEqual(expected, chainedState, $"chained, {length} bytes");
            CollectionAssert.AreEqual(expected, unchainedState, $"unchained, {length} bytes");
        }
    }
}
