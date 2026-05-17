// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadBlockCipherModeTransformExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Extends <see cref="IAeadBlockCipherModeTransform"/> with one-shot encrypt/decrypt overloads that handle the
/// associated-data step, size the output buffer correctly, and return the ciphertext + tag (or recovered plaintext) as
/// a single freshly allocated array.
/// </summary>
/// <remarks>
/// <para>
/// AEAD ("authenticated encryption with associated data") modes — GCM, CCM, EAX, GCM-SIV, AES-SIV, Ascon — encrypt
/// confidential plaintext while simultaneously authenticating the ciphertext together with a separate stream of
/// associated data that travels in the clear (packet headers, message metadata, …). The
/// <see cref="IAeadBlockCipherModeTransform"/> contract gives callers maximum control: invoke
/// <see cref="IAeadBlockCipherModeTransform.ProcessAssociatedData"/>, size the output buffer to
/// <c>plaintext.Length + (TagSize / 8)</c> (or <c>ciphertextWithTag.Length - (TagSize / 8)</c>), call
/// <see cref="IAeadBlockCipherModeTransform.Encrypt"/> or <see cref="IAeadBlockCipherModeTransform.Decrypt"/>, and
/// interpret the return value. This class collapses that fixed sequence into a single call that returns a correctly
/// sized array, matching the shape of <c>AesGcm.Encrypt</c> / <c>AesGcm.Decrypt</c> in the BCL but parameterized over
/// <em>any</em> AEAD transform implementation.
/// </para>
/// <para>
/// The API surface is symmetric and intentionally small:
/// </para>
/// <list type="bullet">
///   <item>
///     <term><c>Encrypt(transform, plaintext)</c></term>
///     <description>One-shot encrypt with no associated data — equivalent to passing
///     <see cref="System.ReadOnlySpan{T}.Empty"/>.</description>
///   </item>
///   <item>
///     <term><c>Encrypt(transform, plaintext, associatedData)</c></term>
///     <description>One-shot encrypt with associated data; returns ciphertext concatenated with the
///     <see cref="IAeadBlockCipherModeTransform.TagSize"/> / 8 byte authentication tag.</description>
///   </item>
///   <item>
///     <term><c>Decrypt(transform, ciphertextWithTag)</c></term>
///     <description>One-shot decrypt with no associated data; throws on tag mismatch.</description>
///   </item>
///   <item>
///     <term><c>Decrypt(transform, ciphertextWithTag, associatedData)</c></term>
///     <description>One-shot decrypt and tag-verify; returns the recovered plaintext, or throws
///     <see cref="CryptographicException"/> on a failed tag check.</description>
///   </item>
/// </list>
/// <para>
/// AEAD transforms are <strong>stateful and single-use within a message</strong>: instantiate a new transform per
/// message, call exactly one of these helpers, and dispose. The associated-data argument must match byte-for-byte
/// between the encrypt and decrypt calls — even a single-bit difference will cause the tag check to fail. Tag
/// verification is constant-time inside the transform implementation, so timing leaks are not a concern. Output arrays
/// are always sized exactly: <c>plaintext.Length + (TagSize / 8)</c> on encrypt,
/// <c>ciphertextWithTag.Length - (TagSize / 8)</c> on decrypt.
/// </para>
/// <example>
/// <code language="csharp">
/// using System.Security.Cryptography;
/// using System.Text;
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// byte[] key    = RandomNumberGenerator.GetBytes(32);
/// byte[] nonce  = RandomNumberGenerator.GetBytes(12);
/// byte[] header = Encoding.UTF8.GetBytes("v=1;msg=42");
///
/// // 1. Encrypt with associated data; receive ciphertext + tag in a single buffer.
/// using IAeadBlockCipherModeTransform enc = new GcmModeTransform(key, nonce);
/// byte[] sealed_ = enc.Encrypt(plaintext, associatedData: header);
///
/// // 2. Decrypt and verify in one call. A tampered tag or header throws CryptographicException.
/// using IAeadBlockCipherModeTransform dec = new GcmModeTransform(key, nonce);
/// byte[] recovered = dec.Decrypt(sealed_, associatedData: header);
///
/// // 3. Same shape with a different mode — Ascon, EAX, GCM-SIV all interchange behind IAeadBlockCipherModeTransform.
/// using IAeadBlockCipherModeTransform enc2 = new AsconAead128(key, nonce);
/// byte[] sealedAscon = enc2.Encrypt(plaintext);
/// </code>
/// </example>
/// </remarks>
/// <seealso href="../guides/cryptography/aead-modes.html">Using AEAD modes (guide with full encrypt / decrypt examples)</seealso>
public static class AeadBlockCipherModeTransformExtensions
{
    /// <summary>
    /// Encrypts <paramref name="plaintext"/> and returns a new byte array containing the
    /// ciphertext concatenated with the authentication tag.
    /// </summary>
    /// <param name="transform">The AEAD transform. Must not be <see langword="null"/>.</param>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <param name="associatedData">
    /// The data to authenticate but not encrypt. May be empty when no associated data is required.
    /// </param>
    /// <returns>
    /// A newly allocated byte array of length <c>plaintext.Length + (transform.TagSize / 8)</c>, containing
    /// the ciphertext followed by the <see cref="IAeadBlockCipherModeTransform.TagSize"/> / 8 byte tag.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="transform"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// The transform has already had its associated data processed or its plaintext / ciphertext consumed.
    /// </exception>
    public static byte[] Encrypt(
        this IAeadBlockCipherModeTransform transform,
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> associatedData)
    {
        if (transform is null) throw new ArgumentNullException(nameof(transform));

        transform.ProcessAssociatedData(associatedData);

        var output = new byte[plaintext.Length + (transform.TagSize / 8)];
        var written = transform.Encrypt(plaintext, output);

        if (written != output.Length)
            Array.Resize(ref output, written);

        return output;
    }

    /// <summary>
    /// Encrypts <paramref name="plaintext"/> with an empty associated-data stream and returns a new
    /// byte array containing the ciphertext concatenated with the authentication tag.
    /// </summary>
    /// <param name="transform">The AEAD transform. Must not be <see langword="null"/>.</param>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <returns>
    /// A newly allocated byte array of length <c>plaintext.Length + (transform.TagSize / 8)</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="transform"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// The transform has already encrypted or decrypted a message. AEAD transforms are single-use
    /// per message — construct a fresh instance.
    /// </exception>
    public static byte[] Encrypt(
        this IAeadBlockCipherModeTransform transform,
        ReadOnlySpan<byte> plaintext)
        => Encrypt(transform, plaintext, []);

    /// <summary>
    /// Verifies <paramref name="ciphertextWithTag"/> against <paramref name="associatedData"/>, decrypts
    /// the ciphertext, and returns the recovered plaintext.
    /// </summary>
    /// <param name="transform">The AEAD transform. Must not be <see langword="null"/>.</param>
    /// <param name="ciphertextWithTag">
    /// The ciphertext followed immediately by the <see cref="IAeadBlockCipherModeTransform.TagSize"/> / 8 byte tag.
    /// Must be at least <c>transform.TagSize / 8</c> bytes long.
    /// </param>
    /// <param name="associatedData">
    /// The data authenticated alongside the ciphertext. Must match what was supplied at encryption time.
    /// </param>
    /// <returns>
    /// A newly allocated byte array of length <c>ciphertextWithTag.Length - (transform.TagSize / 8)</c> containing
    /// the recovered plaintext.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="transform"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="ciphertextWithTag"/> is shorter than <c>transform.TagSize / 8</c> bytes.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// The authentication tag did not verify. The returned buffer is not written in this case.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The transform has already encrypted or decrypted a message, including after a previous
    /// tag-mismatch failure. AEAD transforms are single-use per message — construct a fresh instance.
    /// </exception>
    public static byte[] Decrypt(
        this IAeadBlockCipherModeTransform transform,
        ReadOnlySpan<byte> ciphertextWithTag,
        ReadOnlySpan<byte> associatedData)
    {
        if (transform is null) throw new ArgumentNullException(nameof(transform));

        var tagBytes = transform.TagSize / 8;
        if (ciphertextWithTag.Length < tagBytes)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.Arg_Invalid_InputTooShortForTag, tagBytes),
                nameof(ciphertextWithTag));

        transform.ProcessAssociatedData(associatedData);

        var plaintext = new byte[ciphertextWithTag.Length - tagBytes];
        var written = transform.Decrypt(ciphertextWithTag, plaintext);

        if (written != plaintext.Length)
            Array.Resize(ref plaintext, written);

        return plaintext;
    }

    /// <summary>
    /// Verifies <paramref name="ciphertextWithTag"/> with an empty associated-data stream, decrypts it,
    /// and returns the recovered plaintext.
    /// </summary>
    /// <param name="transform">The AEAD transform. Must not be <see langword="null"/>.</param>
    /// <param name="ciphertextWithTag">
    /// The ciphertext followed immediately by the <see cref="IAeadBlockCipherModeTransform.TagSize"/> / 8 byte tag.
    /// </param>
    /// <returns>A newly allocated byte array containing the recovered plaintext.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="transform"/> is <see langword="null"/>.</exception>
    /// <exception cref="CryptographicException">The authentication tag did not verify.</exception>
    /// <exception cref="InvalidOperationException">
    /// The transform has already encrypted or decrypted a message, including after a previous
    /// tag-mismatch failure. AEAD transforms are single-use per message — construct a fresh instance.
    /// </exception>
    public static byte[] Decrypt(
        this IAeadBlockCipherModeTransform transform,
        ReadOnlySpan<byte> ciphertextWithTag)
        => Decrypt(transform, ciphertextWithTag, []);
}
