// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstHnid.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Discriminates the LTP's <c>HNID</c> union: a 32-bit value that addresses either a heap item (an <c>HID</c>, whose
/// low five type bits are zero) or a subnode (an <c>NID</c>) of the owning node's private namespace.
/// </summary>
internal static class PstHnid
{
    /// <summary>The mask of the low five node-type bits that distinguish an <c>HID</c> from an <c>NID</c>.</summary>
    private const uint TypeMask = 0x1F;

    /// <summary>
    /// Determines whether an <c>HNID</c> is the null value, which carries no data at all.
    /// </summary>
    /// <param name="hnid">The <c>HNID</c> value.</param>
    /// <returns><see langword="true" /> when the value is zero.</returns>
    internal static bool IsNull(uint hnid) =>
        hnid == 0;

    /// <summary>
    /// Determines whether a non-null <c>HNID</c> addresses a heap item rather than a subnode.
    /// </summary>
    /// <param name="hnid">The <c>HNID</c> value.</param>
    /// <returns>
    /// <see langword="true" /> when the value is an <c>HID</c> into the heap; <see langword="false" /> when it is an
    /// <c>NID</c> into the owning node's subnode tree.
    /// </returns>
    internal static bool IsHeapId(uint hnid) =>
        (hnid & TypeMask) == 0;
}
