// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffSheetState.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Identifies the visibility a <c>BOUNDSHEET</c> record declares for its sheet.
/// </summary>
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
