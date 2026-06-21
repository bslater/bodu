// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundArrayDataSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// A <see cref="CompoundDataSource" /> backed by an in-memory byte array, serving spans directly over the array.
/// </summary>
internal sealed class CompoundArrayDataSource
    : CompoundDataSource
{
    /// <summary>The full compound-file byte content.</summary>
    private readonly byte[] _data;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundArrayDataSource" /> class.
    /// </summary>
    /// <param name="data">The full compound-file content.</param>
    public CompoundArrayDataSource(byte[] data) =>
        _data = data;

    /// <inheritdoc />
    public override long Length => _data.Length;

    /// <inheritdoc />
    public override ReadOnlySpan<byte> GetSpan(long offset, int length, Span<byte> scratch) =>
        _data.AsSpan((int)offset, length);

    /// <inheritdoc />
    public override void Read(long offset, Span<byte> destination) =>
        _data.AsSpan((int)offset, destination.Length).CopyTo(destination);
}
