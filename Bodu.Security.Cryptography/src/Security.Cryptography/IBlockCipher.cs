// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IBlockCipher.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Defines a symmetric block cipher that encrypts and decrypts data one fixed-size block at a time.
/// </summary>
/// <remarks>
/// <para>
/// Implementations represent primitives such as AES or Threefish and operate on buffers whose length equals
/// <see cref="BlockSize" />. This interface intentionally exposes only the raw block primitive; chaining modes,
/// padding, and IV management are the responsibility of higher-level components such as
/// <see cref="IBlockCipherModeTransform" /> and <see cref="IPaddingStrategy" />.
/// </para>
/// <para>
/// Implementations must release all sensitive key material when <see cref="IDisposable.Dispose" /> is called and should
/// be safe to invoke repeatedly for the lifetime of the instance.
/// </para>
/// <para>
/// <strong>How this fits with the rest of the library.</strong> <see cref="IBlockCipher" /> is the lowest-level
/// primitive in the cipher stack. Three layers sit on top of it:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// An <see cref="IBlockCipherModeTransform" /> wraps a cipher with a chaining strategy (CBC, CTR, …) — built via
/// <see cref="BlockCipherModeFactory.Create(CipherModeKind, IBlockCipher, byte[])" />.
/// </description>
/// </item>
/// <item>
/// <description>
/// An <see cref="IPaddingStrategy" /> aligns input to the cipher block size — built via
/// <see cref="PaddingFactory.Create(System.Security.Cryptography.PaddingMode)" />.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="BlockCipherTransform" /> bundles a cipher, mode, and padding into a single
/// <see cref="System.Security.Cryptography.ICryptoTransform" /> — the integration point used by every
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm" /> in this library.
/// </description>
/// </item>
/// </list>
/// <para>
/// Most callers never instantiate <see cref="IBlockCipher" /> directly — they use a
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm" /> (Aes, Twofish, Camellia, Threefish, Serpent,
/// Skipjack, Blowfish), set Key/IV/Mode/Padding, and let the library compose the layers. Direct use of this interface
/// is appropriate when implementing a new mode, plugging a non-<c>SymmetricAlgorithm</c> cipher engine into the
/// existing mode infrastructure, or building higher-level constructions on top.
/// </para>
/// </remarks>
/// <seealso cref="IBlockCipherModeTransform"/> <seealso cref="IAeadBlockCipherModeTransform"/>
/// <seealso cref="BlockCipherTransform"/>
public interface IBlockCipher
    : System.IDisposable
{
    /// <summary>
    /// Gets the block size, in bits, of the cipher (for example, 128 bits / 16 bytes for AES).
    /// </summary>
    /// <value>The block size, in bits.</value>
    /// <returns>The block size in bits.</returns>
    /// <remarks>
    /// The block size is expressed in bits to align with the BCL convention used by
    /// <see cref="System.Security.Cryptography.SymmetricAlgorithm.BlockSize" />. Byte-array operations (encrypt,
    /// decrypt, slice) convert to bytes at the call site as <c>BlockSize / 8</c>.
    /// </remarks>
    int BlockSize { get; }

    /// <summary>
    /// Decrypts a single block of ciphertext into the specified output span.
    /// </summary>
    /// <param name="input">
    /// A read-only span containing the ciphertext block. Its byte length must equal <see cref="BlockSize" /> / 8.
    /// </param>
    /// <param name="output">
    /// A writable span that receives the plaintext block. Its byte length must equal <see cref="BlockSize" /> / 8.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown if the length of <paramref name="input" /> or <paramref name="output" /> does not match
    /// <see cref="BlockSize" />.
    /// </exception>
    /// <remarks>
    /// In-place decryption (passing the same buffer as both <paramref name="input" /> and <paramref name="output" />)
    /// is supported only when the implementation explicitly permits it; otherwise the spans must not overlap.
    /// </remarks>
    void Decrypt(ReadOnlySpan<byte> input, Span<byte> output);

    /// <summary>
    /// Encrypts a single block of plaintext into the specified output span.
    /// </summary>
    /// <param name="input">
    /// A read-only span containing the plaintext block. Its byte length must equal <see cref="BlockSize" /> / 8.
    /// </param>
    /// <param name="output">
    /// A writable span that receives the ciphertext block. Its byte length must equal <see cref="BlockSize" /> / 8.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown if the length of <paramref name="input" /> or <paramref name="output" /> does not match
    /// <see cref="BlockSize" />.
    /// </exception>
    /// <remarks>
    /// In-place encryption (passing the same buffer as both <paramref name="input" /> and <paramref name="output" />)
    /// is supported only when the implementation explicitly permits it; otherwise the spans must not overlap.
    /// </remarks>
    void Encrypt(ReadOnlySpan<byte> input, Span<byte> output);
}
