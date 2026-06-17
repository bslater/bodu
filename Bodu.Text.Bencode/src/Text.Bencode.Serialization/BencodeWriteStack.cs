// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeWriteStack.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Carries the serializer's failure state across the recursive converter calls of a single write, so an over-deep graph
/// is reported cooperatively rather than by throwing from deep in the recursion.
/// </summary>
/// <remarks>
/// <para>
/// A container converter records an over-deep graph here and returns, instead of throwing from the deepest writer
/// frame. The single recorded failure is thrown once by <see cref="BencodeSerializerEngine.Serialize{T}" /> after
/// control has returned to the root, so the call stack unwinds through normal returns and cannot exhaust a constrained
/// stack while dispatching the exception.
/// </para>
/// <para>
/// Depth is intentionally <em>not</em> tracked here. The writer is the single owner of container depth (
/// <c>Utf8BencodeWriter.CurrentDepth</c>); a converter consults it directly before opening a container, mirroring how
/// <c>System.Text.Json</c>'s serializer reads <c>Utf8JsonWriter.CurrentDepth</c>. Unlike the TOML twin this carries no
/// path or reference state: <see cref="BencodeSerializationException" /> has no member path, and Bencode performs no
/// object-cycle detection.
/// </para>
/// </remarks>
internal sealed class BencodeWriteStack
{
    /// <summary>The first failure recorded during the write, or <see langword="null" /> while none has occurred.</summary>
    private BencodeSerializationException? _failure;

    /// <summary>
    /// Gets a value indicating whether a failure has been recorded, after which converters cooperatively stop work and
    /// unwind.
    /// </summary>
    /// <returns><see langword="true" /> when a failure has been recorded; otherwise <see langword="false" />.</returns>
    internal bool HasFailure => _failure is not null;

    /// <summary>
    /// Records a failure. Only the first failure is retained.
    /// </summary>
    /// <param name="message">The fully formatted, culture-aware failure message.</param>
    internal void SetFailure(string message) =>
        _failure ??= new BencodeSerializationException(message);

    /// <summary>
    /// Throws the recorded failure, if any. Called once at the root of the write after control has safely returned.
    /// </summary>
    /// <exception cref="BencodeSerializationException">Thrown when a failure was recorded during the write.</exception>
    internal void ThrowIfFailed()
    {
        if (_failure is { } failure)
            throw failure;
    }
}
