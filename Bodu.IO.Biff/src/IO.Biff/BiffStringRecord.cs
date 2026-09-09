// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffStringRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Represents a decoded <c>STRING</c> record: the cached text result of the <c>FORMULA</c> record that precedes it. The
/// text is a 16-bit-length Unicode string in BIFF8 and a 16-bit-length code-page byte string in BIFF5.
/// </summary>
public readonly ref struct BiffStringRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BiffStringRecord" /> struct.
    /// </summary>
    /// <param name="text">The cached text.</param>
    private BiffStringRecord(BiffString text)
    {
        Text = text;
    }

    /// <summary>
    /// Gets the cached text.
    /// </summary>
    /// <value>The text as a span-backed view.</value>
    public BiffString Text { get; }

    /// <summary>
    /// Decodes a <c>STRING</c> payload.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="version">The stream version, which selects the text representation.</param>
    /// <param name="codePage">The code page for BIFF5 text.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    internal static BiffStringRecord Read(ReadOnlySpan<byte> payload, BiffVersion version, int codePage) =>
        new(BiffString.Read(payload, 0, wideLength: true, version, codePage, BiffRecordType.String));
}
