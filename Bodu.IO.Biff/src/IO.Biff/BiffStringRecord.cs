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
/// <remarks>
/// The record identifier is <see cref="BiffRecordType.String" />. The payload is the string structure alone. The record
/// has no position of its own: it belongs to the <see cref="BiffFormulaRecord" /> immediately before it, whose
/// <see cref="BiffFormulaRecord.CachedResultKind" /> is <see cref="BiffCachedResultKind.String" />.
/// </remarks>
/// <seealso cref="BiffReader.GetString" /> <seealso cref="BiffWriter.WriteString(ReadOnlySpan{char})" />
/// <seealso cref="BiffFormulaRecord" />
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
    /// <remarks>
    /// In BIFF8 the text is an <c>XLUnicodeString</c>: only bit 0 of its flags byte, which selects 16-bit characters,
    /// is read, and the other bits are ignored, so the view carries no rich-text runs or extended data.
    /// </remarks>
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
