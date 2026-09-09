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
/// <param name="TotalCount">The number of <c>LABELSST</c> references across the workbook (<c>cstTotal</c>).</param>
/// <param name="UniqueCount">The number of strings in the table (<c>cstUnique</c>).</param>
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
