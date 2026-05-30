// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IStreamCipher.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Defines a synchronous, additive stream cipher that produces a key- and nonce-dependent keystream in fixed-size
/// blocks.
/// </summary>
/// <remarks>
/// <para>
/// Implementations represent primitives such as ChaCha20. Unlike <see cref="IBlockCipher" />, a stream cipher never
/// observes plaintext: it emits a pseudo-random keystream that the caller combines with the message by XOR.
/// Confidentiality therefore depends entirely on never reusing a <c>(key, nonce, counter)</c> triple, because the XOR
/// of two ciphertexts produced under the same keystream reveals the XOR of their plaintexts.
/// </para>
/// <para>
/// The key and nonce are bound when the implementation is constructed, mirroring the way an <see cref="IBlockCipher" />
/// binds its key. Each call to <see cref="GenerateKeystreamBlock(uint, System.Span{byte})" /> produces the keystream
/// block for a single, explicit block counter, so the primitive is stateless across calls and supports random-access
/// (seekable) keystream generation. Per-message counter sequencing, partial-block buffering, and overflow detection
/// are the responsibility of the higher-level <see cref="StreamCipherTransform" />.
/// </para>
/// <para>
/// <strong>How this fits with the rest of the library.</strong> <see cref="IStreamCipher" /> is the stream-cipher
/// counterpart to <see cref="IBlockCipher" />. A <see cref="StreamCipherTransform" /> wraps it to satisfy the
/// <see cref="System.Security.Cryptography.ICryptoTransform" /> contract, and each
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm" /> (for example <see cref="ChaCha20" /> and
/// <see cref="XChaCha20" />) composes the two so callers can use it through a <c>CryptoStream</c> like any other
/// algorithm in this library.
/// </para>
/// <para>
/// Implementations must release all sensitive key material when <see cref="System.IDisposable.Dispose" /> is called.
/// </para>
/// </remarks>
/// <seealso cref="IBlockCipher" />
/// <seealso cref="StreamCipherTransform" />
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
    /// Generates the keystream block for the specified block counter into the supplied span.
    /// </summary>
    /// <param name="counter">The 32-bit block counter selecting which keystream block to produce.</param>
    /// <param name="destination">
    /// A writable span that receives the keystream. Its length must be at least <see cref="BlockSize" />.
    /// </param>
    /// <exception cref="System.ArgumentException">
    /// Thrown if <paramref name="destination" /> is shorter than <see cref="BlockSize" />.
    /// </exception>
    /// <remarks>
    /// The keystream depends only on the bound key and nonce and the supplied <paramref name="counter" />; it is
    /// independent of any message data. Producing the same counter twice yields the same keystream, which is what makes
    /// the cipher self-inverse but also why a counter value must be used at most once per <c>(key, nonce)</c> pair.
    /// </remarks>
    void GenerateKeystreamBlock(uint counter, System.Span<byte> destination);
}
