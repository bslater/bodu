// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffXfRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Represents the leading fields of a decoded <c>XF</c> (extended format) record: the font and number-format indices
/// and the protection and style flags. Alignment, border, and fill fields that follow are not interpreted.
/// </summary>
/// <remarks>
/// The first six bytes are laid out identically in BIFF5 (16-byte record) and BIFF8 (20-byte record).
/// </remarks>
/// <param name="FontIndex">The index of the cell's font in the <c>FONT</c> record sequence.</param>
/// <param name="FormatIndex">The index of the cell's number format.</param>
/// <param name="TypeField">
/// The raw type-and-protection word: locked (bit 0), hidden (bit 1), style (bit 2), parent style index (bits 4–15).
/// </param>
public readonly record struct BiffXfRecord(ushort FontIndex, ushort FormatIndex, ushort TypeField)
{
    /// <summary>The number of leading bytes a record must carry: the font and format indices.</summary>
    internal const int MinimumLength = 4;

    /// <summary>The payload length of a BIFF5 record.</summary>
    internal const int Biff5Length = 16;

    /// <summary>The payload length of a BIFF8 record.</summary>
    internal const int Biff8Length = 20;

    /// <summary>
    /// Gets a value indicating whether the cell is locked.
    /// </summary>
    /// <value><see langword="true" /> when bit 0 of <see cref="TypeField" /> is set.</value>
    public bool IsLocked => (TypeField & 0x0001) != 0;

    /// <summary>
    /// Gets a value indicating whether the cell's formula is hidden.
    /// </summary>
    /// <value><see langword="true" /> when bit 1 of <see cref="TypeField" /> is set.</value>
    public bool IsHidden => (TypeField & 0x0002) != 0;

    /// <summary>
    /// Gets a value indicating whether the record describes a style rather than a cell format.
    /// </summary>
    /// <value><see langword="true" /> when bit 2 of <see cref="TypeField" /> is set.</value>
    public bool IsStyle => (TypeField & 0x0004) != 0;

    /// <summary>
    /// Gets the index of the parent style format.
    /// </summary>
    /// <value>Bits 4–15 of <see cref="TypeField" />; <c>0xFFF</c> for a style record with no parent.</value>
    public int ParentStyleIndex => TypeField >> 4;

    /// <summary>
    /// Decodes an <c>XF</c> payload.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="BiffFormatException">Thrown when the payload is shorter than four bytes.</exception>
    /// <remarks>
    /// A conformant record is 16 (BIFF5) or 20 (BIFF8) bytes; a record carrying only the two indices is tolerated with
    /// a zero <see cref="TypeField" /> so a producer that truncates the record still yields its number format.
    /// </remarks>
    internal static BiffXfRecord Read(ReadOnlySpan<byte> payload)
    {
        const BiffRecordType type = BiffRecordType.Xf;
        BiffPayload.RequireLength(payload, MinimumLength, type);

        return new BiffXfRecord(
            BiffPayload.ReadUInt16(payload, 0, type),
            BiffPayload.ReadUInt16(payload, 2, type),
            payload.Length >= 6 ? BiffPayload.ReadUInt16(payload, 4, type) : (ushort)0);
    }
}
