// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstPcEntry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Represents one property-context record before its value is resolved: the property identifier, its wire type code,
/// and the raw value dword (an inline value or an <c>HNID</c> reference, per the type's storage classification).
/// </summary>
/// <param name="PropertyId">The 16-bit property identifier.</param>
/// <param name="WireType">The 16-bit property wire type code.</param>
/// <param name="RawValue">The record's value dword.</param>
internal readonly record struct PstPcEntry(
    ushort PropertyId,
    ushort WireType,
    uint RawValue);
