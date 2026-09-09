// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffRowRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Represents a decoded <c>ROW</c> record: a row's used column extent, height, option flags, and default format.
/// </summary>
/// <remarks>
/// The record identifier is <see cref="BiffRecordType.Row" />. The payload is sixteen bytes in both versions; the two
/// reserved words at offsets 8 through 11 are not interpreted. The derived properties mask the raw fields:
/// <see cref="Height" />, <see cref="HasCustomHeight" />, <see cref="IsHidden" />, <see cref="OutlineLevel" />,
/// <see cref="HasFormat" />, and <see cref="XfIndex" />.
/// </remarks>
/// <param name="Row">The zero-based row index.</param>
/// <param name="FirstColumn">The zero-based index of the first defined cell in the row.</param>
/// <param name="LastColumnExclusive">One past the zero-based index of the last defined cell in the row.</param>
/// <param name="HeightField">The raw <c>miyRw</c> field: the height in twips in its low 15 bits.</param>
/// <param name="Options">The raw option flags (<c>grbit</c>).</param>
/// <param name="XfField">The raw format field: the extended-format index in its low 12 bits.</param>
/// <seealso cref="BiffReader.GetRow" /> <seealso cref="BiffWriter.WriteRow(in BiffRowRecord)" />
public readonly record struct BiffRowRecord(
    int Row,
    int FirstColumn,
    int LastColumnExclusive,
    ushort HeightField,
    ushort Options,
    ushort XfField)
{
    /// <summary>The payload length of the record.</summary>
    internal const int Length = 16;

    /// <summary>
    /// Gets the row height in twips (twentieths of a point).
    /// </summary>
    /// <value>The low 15 bits of <see cref="HeightField" />.</value>
    public int Height => HeightField & 0x7FFF;

    /// <summary>
    /// Gets a value indicating whether the row height differs from the sheet default.
    /// </summary>
    /// <value><see langword="true" /> when the custom-height option flag (bit 6) is set.</value>
    public bool HasCustomHeight => (Options & 0x0040) != 0;

    /// <summary>
    /// Gets a value indicating whether the row is hidden.
    /// </summary>
    /// <value><see langword="true" /> when the collapsed/hidden option flag (bit 5) is set.</value>
    public bool IsHidden => (Options & 0x0020) != 0;

    /// <summary>
    /// Gets the outline level of the row.
    /// </summary>
    /// <value>The low three bits of <see cref="Options" />.</value>
    public int OutlineLevel => Options & 0x0007;

    /// <summary>
    /// Gets a value indicating whether the row carries an explicit default format.
    /// </summary>
    /// <value><see langword="true" /> when the format option flag (bit 7) is set.</value>
    public bool HasFormat => (Options & 0x0080) != 0;

    /// <summary>
    /// Gets the extended-format index applied to cells the row does not define individually.
    /// </summary>
    /// <value>The low 12 bits of <see cref="XfField" />.</value>
    public ushort XfIndex => (ushort)(XfField & 0x0FFF);

    /// <summary>
    /// Decodes a <c>ROW</c> payload.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="BiffFormatException">Thrown when the payload is shorter than sixteen bytes.</exception>
    internal static BiffRowRecord Read(ReadOnlySpan<byte> payload)
    {
        const BiffRecordType type = BiffRecordType.Row;
        BiffPayload.RequireLength(payload, Length, type);

        return new BiffRowRecord(
            BiffPayload.ReadUInt16(payload, 0, type),
            BiffPayload.ReadUInt16(payload, 2, type),
            BiffPayload.ReadUInt16(payload, 4, type),
            BiffPayload.ReadUInt16(payload, 6, type),
            BiffPayload.ReadUInt16(payload, 12, type),
            BiffPayload.ReadUInt16(payload, 14, type));
    }
}
