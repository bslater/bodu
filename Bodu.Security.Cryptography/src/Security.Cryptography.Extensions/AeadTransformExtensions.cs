// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadTransformExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Provides convenience helpers over <see cref="IAeadTransform" /> that size the output buffer and return a freshly
/// allocated array, for callers who prefer arrays to caller-supplied spans.
/// </summary>
public static class AeadTransformExtensions
{
    /// <summary>
    /// Encrypts <paramref name="plaintext" /> and returns a new array containing the ciphertext followed by the
    /// authentication tag.
    /// </summary>
    /// <param name="transform">The AEAD transform. Must not be <see langword="null" />.</param>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <param name="associatedData">The data authenticated but not encrypted. Defaults to empty.</param>
    /// <returns>A new array of length <c>plaintext.Length + (transform.TagSize / 8)</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="transform" /> is <see langword="null" />.</exception>
    public static byte[] Encrypt(
        this IAeadTransform transform,
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> associatedData = default)
    {
        if (transform is null) throw new ArgumentNullException(nameof(transform));

        var output = new byte[plaintext.Length + (transform.TagSize / 8)];
        var written = transform.Encrypt(plaintext, output, associatedData);

        if (written != output.Length)
            Array.Resize(ref output, written);

        return output;
    }

    /// <summary>
    /// Verifies and decrypts <paramref name="ciphertextWithTag" /> and returns a new array containing the recovered
    /// plaintext.
    /// </summary>
    /// <param name="transform">The AEAD transform. Must not be <see langword="null" />.</param>
    /// <param name="ciphertextWithTag">The ciphertext followed by its authentication tag.</param>
    /// <param name="associatedData">
    /// The data that must match what was supplied at encryption time. Defaults to empty.
    /// </param>
    /// <returns>A new array of length <c>ciphertextWithTag.Length - (transform.TagSize / 8)</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="transform" /> is <see langword="null" />.</exception>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// The authentication tag did not match.
    /// </exception>
    public static byte[] Decrypt(
        this IAeadTransform transform,
        ReadOnlySpan<byte> ciphertextWithTag,
        ReadOnlySpan<byte> associatedData = default)
    {
        if (transform is null) throw new ArgumentNullException(nameof(transform));

        var output = new byte[ciphertextWithTag.Length - (transform.TagSize / 8)];
        var written = transform.Decrypt(ciphertextWithTag, output, associatedData);

        if (written != output.Length)
            Array.Resize(ref output, written);

        return output;
    }
}
