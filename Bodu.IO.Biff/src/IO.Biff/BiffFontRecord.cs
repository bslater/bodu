// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffFontRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Represents a decoded <c>FONT</c> record: size, style attributes, color, character set, and face name. The name is an
/// 8-bit-length string in both versions — a Unicode string in BIFF8, a code-page byte string in BIFF5.
/// </summary>
public readonly ref struct BiffFontRecord
{
    /// <summary>The offset of the face name within the payload.</summary>
    private const int NameOffset = 14;

    /// <summary>
    /// Initializes a new instance of the <see cref="BiffFontRecord" /> struct.
    /// </summary>
    /// <param name="height">The font height in twips.</param>
    /// <param name="attributes">The raw attribute flags.</param>
    /// <param name="colorIndex">The palette color index.</param>
    /// <param name="weight">The font weight.</param>
    /// <param name="escapement">The escapement (superscript/subscript) type.</param>
    /// <param name="underline">The underline type.</param>
    /// <param name="family">The font family.</param>
    /// <param name="characterSet">The character set.</param>
    /// <param name="name">The face name.</param>
    private BiffFontRecord(ushort height, ushort attributes, ushort colorIndex, ushort weight, ushort escapement, byte underline, byte family, byte characterSet, BiffString name)
    {
        Height = height;
        Attributes = attributes;
        ColorIndex = colorIndex;
        Weight = weight;
        Escapement = escapement;
        Underline = underline;
        Family = family;
        CharacterSet = characterSet;
        Name = name;
    }

    /// <summary>
    /// Gets the font height in twips (twentieths of a point).
    /// </summary>
    /// <value>The height.</value>
    public ushort Height { get; }

    /// <summary>
    /// Gets the raw attribute flags: italic (bit 1), strikeout (bit 3), outline (bit 4), shadow (bit 5).
    /// </summary>
    /// <value>The flags.</value>
    public ushort Attributes { get; }

    /// <summary>
    /// Gets the index of the font color in the workbook palette.
    /// </summary>
    /// <value>The color index; <c>0x7FFF</c> selects the automatic color.</value>
    public ushort ColorIndex { get; }

    /// <summary>
    /// Gets the font weight.
    /// </summary>
    /// <value>The weight on the Windows scale: 400 normal, 700 bold.</value>
    public ushort Weight { get; }

    /// <summary>
    /// Gets the escapement type.
    /// </summary>
    /// <value>0 none, 1 superscript, 2 subscript.</value>
    public ushort Escapement { get; }

    /// <summary>
    /// Gets the underline type.
    /// </summary>
    /// <value>0 none, 1 single, 2 double, 0x21 single accounting, 0x22 double accounting.</value>
    public byte Underline { get; }

    /// <summary>
    /// Gets the font family.
    /// </summary>
    /// <value>The Windows font family code.</value>
    public byte Family { get; }

    /// <summary>
    /// Gets the character set.
    /// </summary>
    /// <value>The Windows character set code.</value>
    public byte CharacterSet { get; }

    /// <summary>
    /// Gets the face name.
    /// </summary>
    /// <value>The name as a span-backed view.</value>
    public BiffString Name { get; }

    /// <summary>
    /// Gets a value indicating whether the font is italic.
    /// </summary>
    /// <value><see langword="true" /> when bit 1 of <see cref="Attributes" /> is set.</value>
    public bool IsItalic => (Attributes & 0x0002) != 0;

    /// <summary>
    /// Gets a value indicating whether the font is struck out.
    /// </summary>
    /// <value><see langword="true" /> when bit 3 of <see cref="Attributes" /> is set.</value>
    public bool IsStrikeout => (Attributes & 0x0008) != 0;

    /// <summary>
    /// Gets a value indicating whether the font is bold.
    /// </summary>
    /// <value><see langword="true" /> when <see cref="Weight" /> is at least 700.</value>
    public bool IsBold => Weight >= 700;

    /// <summary>
    /// Decodes a <c>FONT</c> payload.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="version">The stream version, which selects the name's representation.</param>
    /// <param name="codePage">The code page for a BIFF5 name.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="BiffFormatException">Thrown when the payload is malformed.</exception>
    internal static BiffFontRecord Read(ReadOnlySpan<byte> payload, BiffVersion version, int codePage)
    {
        const BiffRecordType type = BiffRecordType.Font;
        BiffPayload.RequireLength(payload, NameOffset + 1, type);

        return new BiffFontRecord(
            BiffPayload.ReadUInt16(payload, 0, type),
            BiffPayload.ReadUInt16(payload, 2, type),
            BiffPayload.ReadUInt16(payload, 4, type),
            BiffPayload.ReadUInt16(payload, 6, type),
            BiffPayload.ReadUInt16(payload, 8, type),
            payload[10],
            payload[11],
            payload[12],
            BiffString.Read(payload, NameOffset, wideLength: false, version, codePage, type));
    }
}
