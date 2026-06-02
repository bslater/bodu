// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IAeadTransform.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents an authenticated encryption with associated data (AEAD) transform — the construction-neutral surface
/// shared by block-cipher AEAD modes (<see cref="IAeadBlockCipherModeTransform" />) and stream-cipher AEADs
/// (<see cref="IStreamAeadTransform" />). A single call encrypts or decrypts a message together with its associated
/// data; associated data is optional and defaults to empty.
/// </summary>
/// <remarks>
/// <para>
/// This is the recommended surface for new code: it does not require a separate associated-data step, mirroring the
/// shape of the BCL's <see cref="System.Security.Cryptography.AesGcm" /> and
/// <see cref="System.Security.Cryptography.ChaCha20Poly1305" /> APIs. Implementations remain single-use per message and
/// not thread-safe; construct a fresh instance for every message.
/// </para>
/// <para>
/// The emitted wire format is <c>ciphertext ‖ tag</c> — the ciphertext followed immediately by the
/// <see cref="TagSize" /> / 8 byte authentication tag.
/// </para>
/// </remarks>
public interface IAeadTransform
    : IDisposable
{
    /// <summary>
    /// Gets the authentication-tag size, in bits.
    /// </summary>
    /// <value>The authentication-tag size, in bits.</value>
    /// <returns>The tag size in bits; divide by 8 for the byte length appended to the ciphertext.</returns>
    int TagSize { get; }

    /// <summary>
    /// Encrypts <paramref name="plaintext" />, authenticates it together with <paramref name="associatedData" />, and
    /// writes the ciphertext followed by the authentication tag to <paramref name="output" />.
    /// </summary>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <param name="output">
    /// Receives the ciphertext followed by the <see cref="TagSize" /> / 8 byte tag. Must be at least
    /// <c>plaintext.Length + (TagSize / 8)</c> bytes long.
    /// </param>
    /// <param name="associatedData">The data authenticated but not encrypted. Defaults to empty.</param>
    /// <returns>Total bytes written: <c>plaintext.Length + (TagSize / 8)</c>.</returns>
    /// <exception cref="ArgumentException"><paramref name="output" /> is too small, or the buffers partially overlap.</exception>
    /// <exception cref="InvalidOperationException">The instance has already processed a message.</exception>
    int Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> output, ReadOnlySpan<byte> associatedData = default);

    /// <summary>
    /// Verifies the authentication tag of <paramref name="ciphertextWithTag" /> against
    /// <paramref name="associatedData" /> and writes the recovered plaintext to <paramref name="output" />.
    /// </summary>
    /// <param name="ciphertextWithTag">The ciphertext followed by its <see cref="TagSize" /> / 8 byte tag.</param>
    /// <param name="output">
    /// Receives the recovered plaintext. Must be at least <c>ciphertextWithTag.Length - (TagSize / 8)</c> bytes long.
    /// </param>
    /// <param name="associatedData">The data that must match what was supplied at encryption time. Defaults to empty.</param>
    /// <returns>Bytes written: <c>ciphertextWithTag.Length - (TagSize / 8)</c>.</returns>
    /// <exception cref="CryptographicException">The authentication tag did not match.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="ciphertextWithTag" /> is shorter than the tag, <paramref name="output" /> is too small, or the
    /// buffers partially overlap.
    /// </exception>
    /// <exception cref="InvalidOperationException">The instance has already processed a message.</exception>
    int Decrypt(ReadOnlySpan<byte> ciphertextWithTag, Span<byte> output, ReadOnlySpan<byte> associatedData = default);
}
