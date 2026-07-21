// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadBlockCipherModeTransformExtensions.Detached.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

/// <content> Detached-tag convenience overloads: the ciphertext and the <see cref="AuthenticationTag" /> travel as
/// separate values instead of the combined <c>ciphertext ‖ tag</c> layout, matching protocols that store or transmit
/// the tag out of band. </content>
public static partial class AeadBlockCipherModeTransformExtensions
{
    /// <summary>
    /// Encrypts <paramref name="plaintext" /> and returns the ciphertext and the authentication tag as separate values
    /// (detached-tag layout).
    /// </summary>
    /// <param name="transform">The AEAD transform. Must not be <see langword="null" />.</param>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <param name="associatedData">
    /// The data to authenticate but not encrypt. May be empty when no associated data is required.
    /// </param>
    /// <returns>
    /// A tuple of the newly allocated ciphertext (the same length as <paramref name="plaintext" />) and the
    /// <see cref="AuthenticationTag" /> of <see cref="IAeadTransform.TagSize" /> / 8 bytes.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="transform" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// The transform has already encrypted or decrypted a message. AEAD transforms are single-use per message —
    /// construct a fresh instance.
    /// </exception>
    public static (byte[] Ciphertext, AuthenticationTag Tag) EncryptDetached(
        this IAeadBlockCipherModeTransform transform,
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> associatedData = default)
    {
        if (transform is null) throw new ArgumentNullException(nameof(transform));

        int tagBytes = transform.TagSize / 8;
        byte[] combined = Encrypt(transform, plaintext, associatedData);

        byte[] ciphertext = combined[..^tagBytes];
        AuthenticationTag tag = AuthenticationTag.FromBytes(combined.AsSpan(combined.Length - tagBytes));
        return (ciphertext, tag);
    }

    /// <summary>
    /// Verifies the detached <paramref name="tag" /> over <paramref name="ciphertext" /> and
    /// <paramref name="associatedData" />, decrypts the ciphertext, and returns the recovered plaintext.
    /// </summary>
    /// <param name="transform">The AEAD transform. Must not be <see langword="null" />.</param>
    /// <param name="ciphertext">The ciphertext, without a trailing tag.</param>
    /// <param name="tag">The detached authentication tag produced at encryption time.</param>
    /// <param name="associatedData">
    /// The data authenticated alongside the ciphertext. Must match what was supplied at encryption time.
    /// </param>
    /// <returns>A newly allocated byte array of the same length as <paramref name="ciphertext" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="transform" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="tag" /> is not <see cref="IAeadTransform.TagSize" /> / 8 bytes long.
    /// </exception>
    /// <exception cref="CryptographicException">The authentication tag did not verify.</exception>
    /// <exception cref="InvalidOperationException">
    /// The transform has already encrypted or decrypted a message, including after a previous tag-mismatch failure.
    /// AEAD transforms are single-use per message — construct a fresh instance.
    /// </exception>
    public static byte[] DecryptDetached(
        this IAeadBlockCipherModeTransform transform,
        ReadOnlySpan<byte> ciphertext,
        AuthenticationTag tag,
        ReadOnlySpan<byte> associatedData = default)
    {
        if (transform is null) throw new ArgumentNullException(nameof(transform));
        if (tag.Length != transform.TagSize / 8)
        {
            throw new ArgumentException(
                string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    CryptoResourceStrings.Arg_Invalid_InputTooShortForTag,
                    transform.TagSize / 8),
                nameof(tag));
        }

        // Reassemble the combined layout the core extension expects.
        byte[] combined = new byte[ciphertext.Length + tag.Length];
        ciphertext.CopyTo(combined);
        tag.AsSpan().CopyTo(combined.AsSpan(ciphertext.Length));

        return Decrypt(transform, combined, associatedData);
    }
}
