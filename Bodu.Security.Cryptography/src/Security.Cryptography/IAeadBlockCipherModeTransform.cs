// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IAeadBlockCipherModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents an authenticated encryption with associated data (AEAD) block cipher mode transform
/// that encrypts or decrypts data and produces or verifies an integrity tag.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="IBlockCipherModeTransform" />, which only encrypts or decrypts, AEAD transforms
/// combine confidentiality with data integrity. The caller supplies optional associated data (AAD) that is
/// authenticated but not encrypted, plus plaintext or ciphertext to be transformed. The output includes an
/// authentication tag that binds the ciphertext and AAD together.
/// </para>
/// <para>
/// Usage pattern for encryption:
/// <code>
/// transform.ProcessAssociatedData(aad);
/// int written = transform.Encrypt(plaintext, output); // output = ciphertext || tag
/// </code>
/// Usage pattern for decryption:
/// <code>
/// transform.ProcessAssociatedData(aad);
/// int written = transform.Decrypt(ciphertextWithTag, output); // throws if tag invalid
/// </code>
/// </para>
/// <para>
/// All implementations are stateful and not thread-safe. A new instance must be created for each
/// message.
/// </para>
/// </remarks>
/// <seealso href="../guides/cryptography/aead-modes.html">Using AEAD modes (guide with GCM, CCM, OCB3, SIV, and GCM-SIV examples)</seealso>
/// <seealso cref="AesBlockCipher" />
/// <seealso cref="Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions" />
public interface IAeadBlockCipherModeTransform
{
    /// <summary>Gets the size of the authentication tag produced by this mode, in bytes.</summary>
    int TagSize { get; }

    /// <summary>
    /// Processes associated data (AAD) that will be authenticated but not encrypted.
    /// Must be called before <see cref="Encrypt" /> or <see cref="Decrypt" />.
    /// </summary>
    /// <param name="associatedData">
    /// The bytes to authenticate. May be empty to indicate no associated data.
    /// </param>
    void ProcessAssociatedData(ReadOnlySpan<byte> associatedData);

    /// <summary>
    /// Encrypts <paramref name="plaintext" /> and appends the authentication tag to
    /// <paramref name="output" />.
    /// </summary>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <param name="output">
    /// Receives the ciphertext followed immediately by the <see cref="TagSize" />-byte tag.
    /// Must be at least <c>plaintext.Length + TagSize</c> bytes long.
    /// </param>
    /// <returns>Total bytes written: <c>plaintext.Length + TagSize</c>.</returns>
    /// <exception cref="ArgumentException"><paramref name="output" /> is too small.</exception>
    int Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> output);

    /// <summary>
    /// Decrypts <paramref name="ciphertextWithTag" /> and verifies the authentication tag.
    /// </summary>
    /// <param name="ciphertextWithTag">
    /// The ciphertext followed immediately by the <see cref="TagSize" />-byte authentication tag.
    /// Must be at least <see cref="TagSize" /> bytes long.
    /// </param>
    /// <param name="output">
    /// Receives the decrypted plaintext. Must be at least
    /// <c>ciphertextWithTag.Length - TagSize</c> bytes long.
    /// </param>
    /// <returns>Bytes written: <c>ciphertextWithTag.Length - TagSize</c>.</returns>
    /// <exception cref="CryptographicException">The authentication tag did not match.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="ciphertextWithTag" /> is shorter than <see cref="TagSize" /> bytes, or
    /// <paramref name="output" /> is too small.
    /// </exception>
    int Decrypt(ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output);
}
