// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CfbArrayDataSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.Internal;

/// <summary>
/// A <see cref="CfbDataSource" /> backed by an in-memory byte array, serving spans directly over the array.
/// </summary>
internal sealed class CfbArrayDataSource
    : CfbDataSource
{
    /// <summary>The full compound-file byte content.</summary>
    private readonly byte[] _data;

    /// <summary>
    /// Initializes a new instance of the <see cref="CfbArrayDataSource" /> class.
    /// </summary>
    /// <param name="data">The full compound-file content.</param>
    public CfbArrayDataSource(byte[] data) =>
        _data = data;

    /// <inheritdoc />
    public override long Length => _data.Length;

    /// <inheritdoc />
    public override ReadOnlySpan<byte> GetSpan(long offset, int length, Span<byte> scratch) =>
        _data.AsSpan((int)offset, length);

    /// <inheritdoc />
    public override void Read(long offset, Span<byte> destination) =>
        _data.AsSpan((int)offset, destination.Length).CopyTo(destination);

    /// <inheritdoc />
    public override ValueTask ReadAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled(cancellationToken);

        _data.AsSpan((int)offset, destination.Length).CopyTo(destination.Span);
        return ValueTask.CompletedTask;
    }
}
