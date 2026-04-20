// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadBlockCipherModeTransformExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions
{
    /// <summary>
    /// Provides one-shot convenience wrappers around <see cref="IAeadBlockCipherModeTransform" />
    /// that handle associated-data processing, output buffer sizing, and the ciphertext + tag layout
    /// in a single call.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The raw <see cref="IAeadBlockCipherModeTransform" /> contract requires callers to call
    /// <see cref="IAeadBlockCipherModeTransform.ProcessAssociatedData" /> before <see cref="IAeadBlockCipherModeTransform.Encrypt" />
    /// or <see cref="IAeadBlockCipherModeTransform.Decrypt" />, size the output buffer to
    /// <c>plaintext.Length + TagSize</c> (or <c>ciphertextWithTag.Length - TagSize</c>), and
    /// interpret the return value. These extension methods collapse all of that into a single call
    /// that returns a correctly sized <see cref="byte" /> array.
    /// </para>
    /// <para>
    /// AEAD transforms are stateful and single-use. Construct a new transform per message and pass
    /// it to exactly one of these extension methods.
    /// </para>
    /// </remarks>
    /// <seealso href="../guides/cryptography/aead-modes.html">Using AEAD modes (guide with full encrypt / decrypt examples)</seealso>
    public static class AeadBlockCipherModeTransformExtensions
    {
        /// <summary>
        /// Encrypts <paramref name="plaintext" /> and returns a new byte array containing the
        /// ciphertext concatenated with the authentication tag.
        /// </summary>
        /// <param name="transform">The AEAD transform. Must not be <see langword="null" />.</param>
        /// <param name="plaintext">The data to encrypt.</param>
        /// <param name="associatedData">
        /// The data to authenticate but not encrypt. May be empty when no associated data is required.
        /// </param>
        /// <returns>
        /// A newly allocated byte array of length <c>plaintext.Length + transform.TagSize</c>, containing
        /// the ciphertext followed by the <see cref="IAeadBlockCipherModeTransform.TagSize" />-byte tag.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="transform" /> is <see langword="null" />.</exception>
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

            byte[] output = new byte[plaintext.Length + transform.TagSize];
            int written = transform.Encrypt(plaintext, output);

            if (written != output.Length)
                Array.Resize(ref output, written);

            return output;
        }

        /// <summary>
        /// Encrypts <paramref name="plaintext" /> with an empty associated-data stream and returns a new
        /// byte array containing the ciphertext concatenated with the authentication tag.
        /// </summary>
        /// <param name="transform">The AEAD transform. Must not be <see langword="null" />.</param>
        /// <param name="plaintext">The data to encrypt.</param>
        /// <returns>
        /// A newly allocated byte array of length <c>plaintext.Length + transform.TagSize</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="transform" /> is <see langword="null" />.</exception>
        public static byte[] Encrypt(
            this IAeadBlockCipherModeTransform transform,
            ReadOnlySpan<byte> plaintext)
            => Encrypt(transform, plaintext, ReadOnlySpan<byte>.Empty);

        /// <summary>
        /// Verifies <paramref name="ciphertextWithTag" /> against <paramref name="associatedData" />, decrypts
        /// the ciphertext, and returns the recovered plaintext.
        /// </summary>
        /// <param name="transform">The AEAD transform. Must not be <see langword="null" />.</param>
        /// <param name="ciphertextWithTag">
        /// The ciphertext followed immediately by the <see cref="IAeadBlockCipherModeTransform.TagSize" />-byte tag.
        /// Must be at least <c>transform.TagSize</c> bytes long.
        /// </param>
        /// <param name="associatedData">
        /// The data authenticated alongside the ciphertext. Must match what was supplied at encryption time.
        /// </param>
        /// <returns>
        /// A newly allocated byte array of length <c>ciphertextWithTag.Length - transform.TagSize</c> containing
        /// the recovered plaintext.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="transform" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="ciphertextWithTag" /> is shorter than <c>transform.TagSize</c> bytes.
        /// </exception>
        /// <exception cref="CryptographicException">
        /// The authentication tag did not verify. The returned buffer is not written in this case.
        /// </exception>
        public static byte[] Decrypt(
            this IAeadBlockCipherModeTransform transform,
            ReadOnlySpan<byte> ciphertextWithTag,
            ReadOnlySpan<byte> associatedData)
        {
            if (transform is null) throw new ArgumentNullException(nameof(transform));

            if (ciphertextWithTag.Length < transform.TagSize)
                throw new ArgumentException(
                    $"Input must be at least {transform.TagSize} bytes (the tag size).",
                    nameof(ciphertextWithTag));

            transform.ProcessAssociatedData(associatedData);

            byte[] plaintext = new byte[ciphertextWithTag.Length - transform.TagSize];
            int written = transform.Decrypt(ciphertextWithTag, plaintext);

            if (written != plaintext.Length)
                Array.Resize(ref plaintext, written);

            return plaintext;
        }

        /// <summary>
        /// Verifies <paramref name="ciphertextWithTag" /> with an empty associated-data stream, decrypts it,
        /// and returns the recovered plaintext.
        /// </summary>
        /// <param name="transform">The AEAD transform. Must not be <see langword="null" />.</param>
        /// <param name="ciphertextWithTag">
        /// The ciphertext followed immediately by the <see cref="IAeadBlockCipherModeTransform.TagSize" />-byte tag.
        /// </param>
        /// <returns>A newly allocated byte array containing the recovered plaintext.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="transform" /> is <see langword="null" />.</exception>
        /// <exception cref="CryptographicException">The authentication tag did not verify.</exception>
        public static byte[] Decrypt(
            this IAeadBlockCipherModeTransform transform,
            ReadOnlySpan<byte> ciphertextWithTag)
            => Decrypt(transform, ciphertextWithTag, ReadOnlySpan<byte>.Empty);
    }
}
