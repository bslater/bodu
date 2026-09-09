// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffSheetState.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Identifies the visibility a <c>BOUNDSHEET</c> record declares for its sheet.
/// </summary>
/// <remarks>
/// Values match the low two bits of the <c>grbit</c> byte of a <c>BOUNDSHEET</c> record; the reader masks the byte, so
/// a value outside the enumeration cannot be observed. A very hidden sheet is revealable only through the Excel object
/// model, never through the user interface.
/// </remarks>
/// <seealso cref="BiffBoundSheetRecord.State" />
/// <seealso cref="BiffWriter.WriteBoundSheet(uint, BiffSheetState, BiffSheetType, ReadOnlySpan{char})" />
public enum BiffSheetState : byte
{
    /// <summary>
    /// The sheet is visible.
    /// </summary>
    Visible = 0x00,

    /// <summary>
    /// The sheet is hidden and can be shown from the application's user interface.
    /// </summary>
    Hidden = 0x01,

    /// <summary>
    /// The sheet is hidden and can be shown only programmatically.
    /// </summary>
    VeryHidden = 0x02,
}
