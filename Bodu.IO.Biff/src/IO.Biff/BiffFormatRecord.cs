// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffFormatRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Represents a decoded <c>FORMAT</c> record: a number-format index and its format code. The code is a 16-bit-length
/// Unicode string in BIFF8 and an 8-bit-length code-page byte string in BIFF5.
/// </summary>
public readonly ref struct BiffFormatRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BiffFormatRecord" /> struct.
    /// </summary>
    /// <param name="formatIndex">The format index.</param>
    /// <param name="code">The format code.</param>
    private BiffFormatRecord(ushort formatIndex, BiffString code)
    {
        FormatIndex = formatIndex;
        Code = code;
    }

    /// <summary>
    /// Gets the number-format index that <c>XF</c> records reference.
    /// </summary>
    /// <value>The format index.</value>
    public ushort FormatIndex { get; }

    /// <summary>
    /// Gets the format code string.
    /// </summary>
    /// <value>The code as a span-backed view.</value>
    public BiffString Code { get; }

    /// <summary>
    /// Decodes a <c>FORMAT</c> payload.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="version">The stream version, which selects the code's representation and length width.</param>
    /// <param name="codePage">The code page for a BIFF5 code.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    internal static BiffFormatRecord Read(ReadOnlySpan<byte> payload, BiffVersion version, int codePage)
    {
        const BiffRecordType type = BiffRecordType.Format;
        BiffPayload.RequireLength(payload, 3, type);

        ushort index = BiffPayload.ReadUInt16(payload, 0, type);
        BiffString code = BiffString.Read(payload, 2, wideLength: version == BiffVersion.Biff8, version, codePage, type);

        return new BiffFormatRecord(index, code);
    }
}
