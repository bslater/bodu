// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffSstHeader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Represents the counts at the head of an <c>SST</c> record: the total number of string references in the workbook and
/// the number of unique strings the table holds.
/// </summary>
/// <remarks>
/// The record identifier is <see cref="BiffRecordType.Sst" />; the record exists only in BIFF8. The eight bytes of
/// counts are followed by the strings, which routinely overflow into <c>CONTINUE</c> records; <see
/// cref="BiffReader.GetSstHeader" /> decodes the counts without touching the strings, and <see cref="BiffSstReader"
///  /> reads the strings.
/// </remarks>
/// <param name="TotalCount">The number of <c>LABELSST</c> references across the workbook (<c>cstTotal</c>).</param>
/// <param name="UniqueCount">The number of strings in the table (<c>cstUnique</c>).</param>
/// <seealso cref="BiffReader.GetSstHeader" /> <seealso cref="BiffSstReader" />
/// <seealso cref="BiffWriter.WriteSst(ReadOnlySpan{string}, uint)" />
public readonly record struct BiffSstHeader(uint TotalCount, uint UniqueCount)
{
    /// <summary>The length of the header within the payload.</summary>
    internal const int Length = 8;

    /// <summary>
    /// Decodes the header of an <c>SST</c> payload.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <returns>The decoded header.</returns>
    /// <exception cref="BiffFormatException">Thrown when the payload is shorter than eight bytes.</exception>
    internal static BiffSstHeader Read(ReadOnlySpan<byte> payload)
    {
        const BiffRecordType type = BiffRecordType.Sst;
        BiffPayload.RequireLength(payload, Length, type);

        return new BiffSstHeader(
            BiffPayload.ReadUInt32(payload, 0, type),
            BiffPayload.ReadUInt32(payload, 4, type));
    }
}
