// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FaultingBlockCipher.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Decorates a real <see cref="IBlockCipher" /> with deterministic fault injection: the Nth block operation (counting
/// <see cref="Encrypt" /> and <see cref="Decrypt" /> calls together) throws instead of transforming, simulating a
/// cipher engine that fails mid-transform. Used by the AEAD fault-hardening tests to assert that transforms zero the
/// caller's output buffer before propagating an exception thrown by the underlying cipher.
/// </summary>
/// <remarks>
/// The fault fires <em>before</em> the inner cipher runs, so the faulting call writes nothing. A
/// <paramref name="faultAfterCalls" /> of zero (or negative) disables the fault entirely, turning the decorator into a
/// pure call counter — the fault-sweep tests use that mode to measure how many block operations a clean run performs.
/// </remarks>
internal sealed class FaultingBlockCipher : IBlockCipher
{
    /// <summary>The wrapped cipher performing the real block transforms.</summary>
    private readonly IBlockCipher _inner;

    /// <summary>The 1-based combined call index at which the fault fires; zero or negative disables the fault.</summary>
    private readonly int _faultAfterCalls;

    /// <summary>The combined number of <see cref="Encrypt" /> and <see cref="Decrypt" /> calls made so far.</summary>
    private int _callCount;

    /// <summary>
    /// Initialises a new instance wrapping <paramref name="inner" />, faulting on the
    /// <paramref name="faultAfterCalls" />th block operation.
    /// </summary>
    /// <param name="inner">The real cipher to delegate to. Must not be <see langword="null" />.</param>
    /// <param name="faultAfterCalls">
    /// The 1-based combined call index at which to throw; zero or negative disables fault injection.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="inner" /> is <see langword="null" />.</exception>
    public FaultingBlockCipher(IBlockCipher inner, int faultAfterCalls)
    {
        if (inner is null) throw new ArgumentNullException(nameof(inner));

        _inner = inner;
        _faultAfterCalls = faultAfterCalls;
    }

    /// <summary>
    /// Gets the combined number of <see cref="Encrypt" /> and <see cref="Decrypt" /> calls observed so far, including
    /// the faulting call.
    /// </summary>
    /// <value>The total block-operation count.</value>
    public int CallCount => _callCount;

    /// <inheritdoc />
    public int BlockSize => _inner.BlockSize;

    /// <inheritdoc />
    public void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowIfFaultReached();
        _inner.Encrypt(input, output);
    }

    /// <inheritdoc />
    public void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowIfFaultReached();
        _inner.Decrypt(input, output);
    }

    /// <inheritdoc />
    public void Dispose() =>
        _inner.Dispose();

    /// <summary>
    /// Advances the combined call counter and throws when the configured fault index is reached, before any block
    /// transformation runs.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the faulting call index is reached.</exception>
    private void ThrowIfFaultReached()
    {
        _callCount++;

        if (_faultAfterCalls > 0 && _callCount == _faultAfterCalls)
            throw new InvalidOperationException($"Injected block-cipher fault on call {_callCount}.");
    }
}
