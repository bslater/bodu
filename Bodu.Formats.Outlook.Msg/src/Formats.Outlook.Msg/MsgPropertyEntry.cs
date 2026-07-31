// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgPropertyEntry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook.Msg;

/// <summary>
/// Represents one 16-byte fixed record of an MS-OXMSG property stream.
/// </summary>
/// <remarks>
/// For a fixed-length property, <see cref="Raw" /> holds the little-endian value in its low bytes. For a
/// variable-length or multi-valued property, the low 32 bits record the payload size (including the string terminator
/// for string types) and the payload itself lives in the matching <c>__substg1.0_</c> stream.
/// </remarks>
/// <param name="Tag">The raw 32-bit property tag.</param>
/// <param name="Flags">The property flags (readable / writable / mandatory; unknown bits are preserved).</param>
/// <param name="Raw">The 8-byte value-or-size field, little-endian.</param>
internal readonly record struct MsgPropertyEntry(
    uint Tag,
    uint Flags,
    ulong Raw)
{
    /// <summary>
    /// Gets the recorded payload size for a variable-length or multi-valued property.
    /// </summary>
    /// <value>The low 32 bits of <see cref="Raw" />.</value>
    internal uint Size =>
        (uint)Raw;
}
