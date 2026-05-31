// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IStreamCipher.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Defines a synchronous, additive stream cipher engine that produces a key- and nonce-dependent keystream in
/// fixed-size blocks.
/// </summary>
/// <remarks>
/// <para>
/// Implementations represent primitives such as ChaCha20, Salsa20, Rabbit, and HC-128. Unlike
/// <see cref="IBlockCipher" />, a stream cipher never observes plaintext: it emits a pseudo-random keystream that the
/// caller combines with the message by XOR. Confidentiality therefore depends entirely on never reusing a
/// <c>(key, nonce)</c> pair, because the XOR of two ciphertexts produced under the same keystream reveals the XOR of
/// their plaintexts.
/// </para>
/// <para>
/// The key and nonce are bound when the implementation is constructed, mirroring the way an <see cref="IBlockCipher" />
/// binds its key. The engine owns its own keystream position: each call to
/// <see cref="NextKeystreamBlock(System.Span{byte})" /> emits the next block and advances the internal counter or
/// state. This keeps the abstraction uniform across ciphers with a seekable block counter (ChaCha20, Salsa20) and those
/// whose keystream is produced by an evolving internal state with no random access (Rabbit, HC-128). Partial-block
/// buffering across calls is the responsibility of the higher-level <see cref="StreamCipherTransform" />.
/// </para>
/// <para>
/// <strong>How this fits with the rest of the library.</strong> <see cref="IStreamCipher" /> is the stream-cipher
/// counterpart to <see cref="IBlockCipher" />. A <see cref="StreamCipherTransform" /> wraps it to satisfy the
/// <see cref="System.Security.Cryptography.ICryptoTransform" /> contract, and a <see cref="SymmetricStreamAlgorithm" />
/// composes the two so callers can use it through a <c>CryptoStream</c> like any other algorithm in this library.
/// </para>
/// <para>
/// Implementations must release all sensitive key material when <see cref="System.IDisposable.Dispose" /> is called.
/// </para>
/// </remarks>
/// <seealso cref="IBlockCipher" /> <seealso cref="StreamCipherTransform" />
public interface IStreamCipher
    : System.IDisposable
{
    /// <summary>
    /// Gets the keystream block size, in bytes (for example, 64 bytes for ChaCha20).
    /// </summary>
    /// <value>The keystream block size, in bytes.</value>
    /// <returns>The number of keystream bytes produced by a single block-function evaluation.</returns>
    /// <remarks>
    /// The block size is expressed in bytes — not bits — because stream ciphers operate on byte-granular keystream
    /// segments rather than the bit-oriented block sizes reported by <see cref="IBlockCipher.BlockSize" />.
    /// </remarks>
    int BlockSize { get; }

    /// <summary>
    /// Emits the next keystream block and advances the engine's internal counter or state.
    /// </summary>
    /// <param name="destination">
    /// A writable span that receives the keystream. Its length must be at least <see cref="BlockSize" />.
    /// </param>
    /// <exception cref="System.ArgumentException">
    /// Thrown if <paramref name="destination" /> is shorter than <see cref="BlockSize" />.
    /// </exception>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// Thrown if the keystream is exhausted — for example, a fixed-width block counter would wrap and reuse earlier
    /// keystream. Continuing past this point would compromise confidentiality.
    /// </exception>
    /// <remarks>
    /// The keystream depends only on the bound key and nonce and the number of blocks already emitted; it is
    /// independent of any message data. Because the engine advances its own position, successive calls yield
    /// successive, non-overlapping keystream blocks.
    /// </remarks>
    void NextKeystreamBlock(System.Span<byte> destination);
}
