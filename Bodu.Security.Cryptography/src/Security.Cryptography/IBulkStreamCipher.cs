// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IBulkStreamCipher.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Extends <see cref="IStreamCipher" /> with a bulk entry point that combines many whole keystream blocks with the
/// input in one call, for engines whose keystream can be produced several blocks at a time.
/// </summary>
/// <remarks>
/// The bulk entry point emits exactly the keystream successive calls to
/// <see cref="IStreamCipher.NextKeystreamBlock(Span{byte})" /> would, advances the engine past it, and shares the
/// engine's exhaustion guard. <see cref="StreamCipherTransform" /> and the Poly1305 AEADs use it for whole blocks and
/// fall back to the single-block form for a partial tail and for engines that do not implement it.
/// </remarks>
internal interface IBulkStreamCipher
    : IStreamCipher
{
    /// <summary>
    /// Combines whole blocks of input with the next keystream blocks by XOR and advances the engine past them.
    /// </summary>
    /// <param name="input">The input, a whole number of keystream blocks.</param>
    /// <param name="output">
    /// The destination, at least as long as <paramref name="input" />. It may be the same memory as
    /// <paramref name="input" />, but must not partially overlap it.
    /// </param>
    /// <exception cref="ObjectDisposedException">The engine has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// The length of <paramref name="input" /> is not a multiple of <see cref="IStreamCipher.BlockSize" />, or
    /// <paramref name="output" /> is shorter than <paramref name="input" />.
    /// </exception>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// The keystream ran out before every block was produced. The blocks that remained were written first, and the
    /// engine stays exhausted, exactly as the equivalent calls to
    /// <see cref="IStreamCipher.NextKeystreamBlock(Span{byte})" /> leave it.
    /// </exception>
    void XorKeystreamBlocks(ReadOnlySpan<byte> input, Span<byte> output);
}
