// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8Record.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Biff8;

/// <summary>
/// Represents a single BIFF record yielded by a <see cref="Biff8RecordCursor" />: its typed identifier and a payload
/// slice over the underlying buffer, without copying.
/// </summary>
internal readonly ref struct Biff8Record
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Biff8Record" /> struct.
    /// </summary>
    /// <param name="id">The 16-bit record type identifier.</param>
    /// <param name="payload">The record payload, excluding the four-byte type-and-length header.</param>
    internal Biff8Record(ushort id, ReadOnlySpan<byte> payload)
    {
        Id = id;
        Payload = payload;
    }

    /// <summary>
    /// Gets the 16-bit record type identifier.
    /// </summary>
    /// <returns>The raw record identifier.</returns>
    public ushort Id { get; }

    /// <summary>
    /// Gets the record payload, excluding the four-byte type-and-length header.
    /// </summary>
    /// <returns>A slice of the source buffer containing the record payload.</returns>
    public ReadOnlySpan<byte> Payload { get; }

    /// <summary>
    /// Gets the record type identifier as a <see cref="Biff8RecordType" />.
    /// </summary>
    /// <returns>
    /// The typed record identifier. The value may not correspond to a defined <see cref="Biff8RecordType" /> member
    /// when the record is one this reader does not recognize.
    /// </returns>
    public Biff8RecordType Type => (Biff8RecordType)Id;
}
