// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CbcChain.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides CBC chaining for the modes built on it - CBC encryption, and the CBC-MAC inside CMAC and CCM - through the
/// cipher's own chained implementation where it has one, and a block at a time otherwise.
/// </summary>
internal static class CbcChain
{
    /// <summary>The number of blocks from which a cipher's own chained implementation is used: below it, the per-call cost of a platform chain outweighs single-block calls.</summary>
    internal const int NativeThresholdBlocks = 6;

    /// <summary>
    /// Encrypts whole blocks in CBC mode, starting from <paramref name="chainingValue" /> and leaving the last
    /// ciphertext block in it.
    /// </summary>
    /// <param name="cipher">The block cipher.</param>
    /// <param name="input">The blocks to encrypt, a whole number of blocks.</param>
    /// <param name="output">
    /// Receives the ciphertext, and may be the same memory as <paramref name="input" />; or empty, to discard it.
    /// </param>
    /// <param name="chainingValue">
    /// A block-sized value XORed into the first input block; receives the last ciphertext block.
    /// </param>
    internal static void Encrypt(IBlockCipher cipher, ReadOnlySpan<byte> input, Span<byte> output, Span<byte> chainingValue)
    {
        int blockBytes = chainingValue.Length;
        if (input.IsEmpty)
            return;

        if (cipher is ICbcBlockCipher chained && input.Length >= NativeThresholdBlocks * blockBytes)
        {
            chained.EncryptCbc(input, output, chainingValue);
            return;
        }

        Span<byte> block = stackalloc byte[blockBytes];
        try
        {
            for (int offset = 0; offset < input.Length; offset += blockBytes)
            {
                // Read the input block before any output is written, so exact aliasing is safe.
                CryptographyHelper.Xor(chainingValue, input.Slice(offset, blockBytes), block);
                cipher.Encrypt(block, chainingValue);

                if (!output.IsEmpty)
                    chainingValue.CopyTo(output.Slice(offset, blockBytes));
            }
        }
        finally
        {
            CryptographyHelper.Clear(block);
        }
    }

    /// <summary>
    /// Folds whole blocks into a CBC-MAC state: the state becomes the last block of their CBC encryption from it.
    /// </summary>
    /// <param name="cipher">The block cipher.</param>
    /// <param name="blocks">The blocks to fold in, a whole number of blocks.</param>
    /// <param name="state">The block-sized CBC-MAC state; updated in place.</param>
    internal static void Mac(IBlockCipher cipher, ReadOnlySpan<byte> blocks, Span<byte> state) =>
        Encrypt(cipher, blocks, Span<byte>.Empty, state);
}
