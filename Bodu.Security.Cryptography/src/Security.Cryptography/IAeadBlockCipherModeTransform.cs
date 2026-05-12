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
/// Unlike <see cref="IBlockCipherModeTransform"/>, which only encrypts or decrypts, AEAD transforms
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
/// All implementations are stateful, not thread-safe, and <strong>single-use per message</strong>.
/// A second call to <see cref="Encrypt"/> or <see cref="Decrypt"/> on the same instance — including
/// after a tag-mismatch failure — throws <see cref="System.InvalidOperationException"/>. Construct
/// a fresh transform for every message and dispose it when finished.
/// </para>
/// <para>
/// <strong>API surface.</strong> The library ships several implementations clustered by trade-off:
/// </para>
/// <list type="bullet">
///   <item>
///     <term>Default high-throughput AEAD</term>
///     <description><see cref="GcmModeTransform"/> — single-pass, hardware-accelerated, fragile under nonce reuse.</description>
///   </item>
///   <item>
///     <term>Constrained-environment AEAD</term>
///     <description><see cref="CcmModeTransform"/> — two-pass, no Galois-field arithmetic, used by Zigbee / Bluetooth Mesh.</description>
///   </item>
///   <item>
///     <term>Two-pass alternatives</term>
///     <description><see cref="EaxModeTransform"/> — flexible nonce length, OMAC-based authentication.</description>
///   </item>
///   <item>
///     <term>Misuse-resistant</term>
///     <description><see cref="GcmSivModeTransform"/> (RFC 8452) and <see cref="SivModeTransform"/> (RFC 5297) — nonce reuse only leaks message-equality.</description>
///   </item>
///   <item>
///     <term>Single-pass without GCM's failure profile</term>
///     <description><see cref="OcbModeTransform"/> — RFC 7253, single-pass, graceful nonce-reuse failure.</description>
///   </item>
/// </list>
/// <para>
/// Most callers should reach for the helper methods on
/// <see cref="Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions"/> instead of calling
/// <see cref="ProcessAssociatedData"/> + <see cref="Encrypt"/>/<see cref="Decrypt"/> directly — those wrappers
/// size the output buffer correctly and return a single freshly allocated array.
/// </para>
/// <para>
/// <strong>API surface.</strong> The library ships several implementations clustered by trade-off:
/// </para>
/// <list type="bullet">
///   <item>
///     <term>Default high-throughput AEAD</term>
///     <description><see cref="GcmModeTransform"/> — single-pass, hardware-accelerated, fragile under nonce reuse.</description>
///   </item>
///   <item>
///     <term>Constrained-environment AEAD</term>
///     <description><see cref="CcmModeTransform"/> — two-pass, no Galois-field arithmetic, used by Zigbee / Bluetooth Mesh.</description>
///   </item>
///   <item>
///     <term>Two-pass alternatives</term>
///     <description><see cref="EaxModeTransform"/> — flexible nonce length, OMAC-based authentication.</description>
///   </item>
///   <item>
///     <term>Misuse-resistant</term>
///     <description><see cref="GcmSivModeTransform"/> (RFC 8452) and <see cref="SivModeTransform"/> (RFC 5297) — nonce reuse only leaks message-equality.</description>
///   </item>
///   <item>
///     <term>Single-pass without GCM's failure profile</term>
///     <description><see cref="OcbModeTransform"/> — RFC 7253, single-pass, graceful nonce-reuse failure.</description>
///   </item>
/// </list>
/// <para>
/// Most callers should reach for the helper methods on
/// <see cref="Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions"/> instead of calling
/// <see cref="ProcessAssociatedData"/> + <see cref="Encrypt"/>/<see cref="Decrypt"/> directly — those wrappers
/// size the output buffer correctly and return a single freshly allocated array.
/// </para>
/// </remarks>
/// <seealso href="../guides/cryptography/aead-modes.html">Using AEAD modes (guide with GCM, CCM, OCB3, SIV, and GCM-SIV examples)</seealso>
/// <seealso cref="AesBlockCipher"/>
/// <seealso cref="Bodu.Security.Cryptography.Extensions.AeadBlockCipherModeTransformExtensions"/>
public interface IAeadBlockCipherModeTransform
    : System.IDisposable
{
    /// <summary>Gets the size of the authentication tag produced by this mode, in bytes.</summary>
    int TagSize { get; }

    /// <summary>
    /// Processes associated data (AAD) that will be authenticated but not encrypted.
    /// Must be called before <see cref="Encrypt"/> or <see cref="Decrypt"/>.
    /// </summary>
    /// <param name="associatedData">
    /// The bytes to authenticate. May be empty to indicate no associated data.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Associated data has already been processed on this instance, or the instance has already
    /// completed an <see cref="Encrypt"/> or <see cref="Decrypt"/> operation.
    /// </exception>
    void ProcessAssociatedData(ReadOnlySpan<byte> associatedData);

    /// <summary>
    /// Encrypts <paramref name="plaintext"/> and appends the authentication tag to
    /// <paramref name="output"/>.
    /// </summary>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <param name="output">
    /// Receives the ciphertext followed immediately by the <see cref="TagSize"/>-byte tag.
    /// Must be at least <c>plaintext.Length + TagSize</c> bytes long.
    /// </param>
    /// <returns>Total bytes written: <c>plaintext.Length + TagSize</c>.</returns>
    /// <exception cref="ArgumentException"><paramref name="output"/> is too small.</exception>
    /// <exception cref="InvalidOperationException">
    /// The instance has already encrypted or decrypted a message. AEAD transforms are single-use
    /// per message — construct a fresh instance.
    /// </exception>
    int Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> output);

    /// <summary>
    /// Decrypts <paramref name="ciphertextWithTag"/> and verifies the authentication tag.
    /// </summary>
    /// <param name="ciphertextWithTag">
    /// The ciphertext followed immediately by the <see cref="TagSize"/>-byte authentication tag.
    /// Must be at least <see cref="TagSize"/> bytes long.
    /// </param>
    /// <param name="output">
    /// Receives the decrypted plaintext. Must be at least
    /// <c>ciphertextWithTag.Length - TagSize</c> bytes long.
    /// </param>
    /// <returns>Bytes written: <c>ciphertextWithTag.Length - TagSize</c>.</returns>
    /// <exception cref="CryptographicException">The authentication tag did not match.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="ciphertextWithTag"/> is shorter than <see cref="TagSize"/> bytes, or
    /// <paramref name="output"/> is too small.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The instance has already encrypted or decrypted a message, including after a previous
    /// tag-mismatch failure. AEAD transforms are single-use per message — construct a fresh instance.
    /// </exception>
    int Decrypt(ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output);
}
