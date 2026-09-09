// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffFormulaRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.IO.Biff;

/// <summary>
/// Represents a decoded <c>FORMULA</c> record: the cell's position and format, the cached result of its last
/// calculation, its option flags, and the parsed-expression tokens, which the codec exposes as raw bytes.
/// </summary>
/// <remarks>
/// The eight-byte result field holds a double unless its last two bytes are <c>0xFFFF</c>, in which case its first byte
/// selects the kind (<see cref="CachedResultKind" />) and a string result is carried by the <c>STRING</c> record that
/// immediately follows. The layout is identical in BIFF5 and BIFF8.
/// </remarks>
public readonly ref struct BiffFormulaRecord
{
    /// <summary>The smallest payload the codec accepts: position, format, and result.</summary>
    internal const int MinimumLength = 14;

    /// <summary>The offset of the option flags.</summary>
    private const int FlagsOffset = 14;

    /// <summary>The offset of the token length.</summary>
    private const int TokenLengthOffset = 20;

    /// <summary>The offset of the tokens.</summary>
    private const int TokensOffset = 22;

    /// <summary>
    /// Initializes a new instance of the <see cref="BiffFormulaRecord" /> struct.
    /// </summary>
    /// <param name="row">The zero-based row index.</param>
    /// <param name="column">The zero-based column index.</param>
    /// <param name="xfIndex">The extended-format index of the cell.</param>
    /// <param name="kind">The kind of cached result.</param>
    /// <param name="numberValue">
    /// The cached number, when the kind is <see cref="BiffCachedResultKind.Number" />.
    /// </param>
    /// <param name="rawValue">The cached boolean or error byte.</param>
    /// <param name="flags">The option flags.</param>
    /// <param name="tokens">The parsed-expression tokens.</param>
    private BiffFormulaRecord(int row, int column, ushort xfIndex, BiffCachedResultKind kind, double numberValue, byte rawValue, ushort flags, ReadOnlySpan<byte> tokens)
    {
        Row = row;
        Column = column;
        XfIndex = xfIndex;
        CachedResultKind = kind;
        NumberValue = numberValue;
        RawValue = rawValue;
        Flags = flags;
        Tokens = tokens;
    }

    /// <summary>
    /// Gets the zero-based row index.
    /// </summary>
    /// <value>The row index.</value>
    public int Row { get; }

    /// <summary>
    /// Gets the zero-based column index.
    /// </summary>
    /// <value>The column index.</value>
    public int Column { get; }

    /// <summary>
    /// Gets the extended-format index of the cell.
    /// </summary>
    /// <value>The XF index.</value>
    public ushort XfIndex { get; }

    /// <summary>
    /// Gets the kind of cached result the record carries.
    /// </summary>
    /// <value>The result kind.</value>
    public BiffCachedResultKind CachedResultKind { get; }

    /// <summary>
    /// Gets the cached numeric result.
    /// </summary>
    /// <value>
    /// The number; meaningful only when <see cref="CachedResultKind" /> is <see cref="BiffCachedResultKind.Number" />.
    /// </value>
    public double NumberValue { get; }

    /// <summary>
    /// Gets the cached boolean or error byte.
    /// </summary>
    /// <value>
    /// The value byte; meaningful only when <see cref="CachedResultKind" /> is
    /// <see cref="BiffCachedResultKind.Boolean" /> or <see cref="BiffCachedResultKind.Error" />.
    /// </value>
    public byte RawValue { get; }

    /// <summary>
    /// Gets a value indicating whether the cached boolean result is <see langword="true" />.
    /// </summary>
    /// <value><see langword="true" /> when the value byte is non-zero.</value>
    public bool BooleanValue => RawValue != 0;

    /// <summary>
    /// Gets the cached error code.
    /// </summary>
    /// <value>The BIFF error code.</value>
    public byte ErrorCode => RawValue;

    /// <summary>
    /// Gets the option flags (<c>grbit</c>): bit 0 always-calculate, bit 1 calculate-on-load, bit 3 shared formula.
    /// </summary>
    /// <value>The raw flags, or zero when the record omits them.</value>
    public ushort Flags { get; }

    /// <summary>
    /// Gets the parsed-expression tokens (<c>rgce</c>) as raw bytes. The codec does not interpret them.
    /// </summary>
    /// <value>The token bytes, or an empty span when the record omits them.</value>
    public ReadOnlySpan<byte> Tokens { get; }

    /// <summary>
    /// Decodes a <c>FORMULA</c> payload.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="BiffFormatException">
    /// Thrown when the payload is shorter than fourteen bytes, or declares more token bytes than it holds.
    /// </exception>
    internal static BiffFormulaRecord Read(ReadOnlySpan<byte> payload)
    {
        const BiffRecordType type = BiffRecordType.Formula;
        BiffPayload.RequireLength(payload, MinimumLength, type);

        int row = BiffPayload.ReadUInt16(payload, 0, type);
        int column = BiffPayload.ReadUInt16(payload, 2, type);
        ushort xfIndex = BiffPayload.ReadUInt16(payload, 4, type);
        ReadOnlySpan<byte> result = payload.Slice(6, 8);

        BiffCachedResultKind kind;
        double number = 0;
        byte raw = 0;

        // A trailing 0xFFFF marks a non-numeric cached result; the leading byte selects which kind.
        if (result[6] == 0xFF && result[7] == 0xFF)
        {
            switch (result[0])
            {
                case 0:
                    kind = BiffCachedResultKind.String;
                    break;
                case 1:
                    kind = BiffCachedResultKind.Boolean;
                    raw = result[2];
                    break;
                case 2:
                    kind = BiffCachedResultKind.Error;
                    raw = result[2];
                    break;
                default:
                    kind = BiffCachedResultKind.Empty;
                    break;
            }
        }
        else
        {
            kind = BiffCachedResultKind.Number;
            number = BinaryPrimitives.ReadDoubleLittleEndian(result);
        }

        ushort flags = 0;
        ReadOnlySpan<byte> tokens = default;
        if (payload.Length >= TokensOffset)
        {
            flags = BiffPayload.ReadUInt16(payload, FlagsOffset, type);
            int tokenLength = BiffPayload.ReadUInt16(payload, TokenLengthOffset, type);
            if (TokensOffset + tokenLength > payload.Length)
                throw BiffPayload.Malformed(type);

            tokens = payload.Slice(TokensOffset, tokenLength);
        }

        return new BiffFormulaRecord(row, column, xfIndex, kind, number, raw, flags, tokens);
    }
}
