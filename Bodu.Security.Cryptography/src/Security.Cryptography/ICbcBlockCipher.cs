// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICbcBlockCipher.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Chains CBC encryption through a block cipher's own implementation, for a cipher that can run a whole chain faster
/// than a loop of single-block calls.
/// </summary>
/// <remarks>
/// <para>
/// CBC encryption is sequential - each block is encrypted after the previous ciphertext block is XORed into it - so
/// <see cref="IBlockCipher.EncryptBlocks" /> cannot express it. A cipher backed by a platform implementation, such as
/// <see cref="AesBlockCipher" />, can run the chain in one platform call; the CBC-MAC modes (CMAC, CCM) and CBC
/// encryption use this interface through <see cref="CbcChain" /> when the cipher offers it.
/// </para>
/// <para>
/// The interface is internal and implemented explicitly, so it adds nothing to a cipher's public surface.
/// </para>
/// </remarks>
internal interface ICbcBlockCipher
{
    /// <summary>
    /// Encrypts whole blocks in CBC mode, starting from <paramref name="chainingValue" /> and leaving the last
    /// ciphertext block in it.
    /// </summary>
    /// <param name="input">The blocks to encrypt: a whole, non-zero number of blocks.</param>
    /// <param name="output">
    /// Receives the ciphertext, and may be the same memory as <paramref name="input" />; or empty, to discard it, as a
    /// CBC-MAC does.
    /// </param>
    /// <param name="chainingValue">
    /// The block XORed into the first input block; receives the last ciphertext block.
    /// </param>
    void EncryptCbc(ReadOnlySpan<byte> input, Span<byte> output, Span<byte> chainingValue);
}
