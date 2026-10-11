// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UnbackedMemoryManager.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.IO.Compound;

/// <summary>
/// Provides a <see cref="Memory{T}" /> that reports a length without holding the bytes, so that a test can hand a
/// writer a payload of more than 2 GB without allocating it.
/// </summary>
/// <remarks>
/// <see cref="Memory{T}.Length" /> is the length given to the constructor, but the manager's span is empty, so reading
/// the memory's <see cref="Memory{T}.Span" /> throws <see cref="ArgumentOutOfRangeException" />. A writer that rejects
/// the payload by its length passes; one that starts to copy it fails at once instead of copying gigabytes.
/// </remarks>
internal sealed class UnbackedMemoryManager : MemoryManager<byte>
{
    /// <summary>The length the memory reports.</summary>
    private readonly int _length;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnbackedMemoryManager" /> class.
    /// </summary>
    /// <param name="length">The length the memory reports, in bytes.</param>
    public UnbackedMemoryManager(int length)
    {
        _length = length;
    }

    /// <inheritdoc />
    public override Memory<byte> Memory => CreateMemory(_length);

    /// <inheritdoc />
    public override Span<byte> GetSpan() =>
        Span<byte>.Empty;

    /// <inheritdoc />
    public override MemoryHandle Pin(int elementIndex = 0) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    public override void Unpin()
    {
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
    }
}
