// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SingleBlockStreamCipher.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Wraps a stream cipher engine behind <see cref="IStreamCipher" /> alone, hiding any bulk entry point, so a consumer
/// takes its one-block-at-a-time path; the differential tests hold the bulk path to it.
/// </summary>
internal sealed class SingleBlockStreamCipher
    : IStreamCipher
{
    /// <summary>The wrapped engine.</summary>
    private readonly IStreamCipher _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleBlockStreamCipher" /> class over an engine.
    /// </summary>
    /// <param name="inner">The engine to wrap, which the wrapper disposes.</param>
    internal SingleBlockStreamCipher(IStreamCipher inner)
    {
        _inner = inner;
    }

    /// <inheritdoc />
    public int BlockSize => _inner.BlockSize;

    /// <inheritdoc />
    public void NextKeystreamBlock(Span<byte> destination) =>
        _inner.NextKeystreamBlock(destination);

    /// <inheritdoc />
    public void Dispose() =>
        _inner.Dispose();
}
