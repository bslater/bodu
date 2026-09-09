// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffSheetType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Identifies the kind of sheet a <c>BOUNDSHEET</c> record describes.
/// </summary>
public enum BiffSheetType : byte
{
    /// <summary>
    /// A worksheet or dialog sheet.
    /// </summary>
    Worksheet = 0x00,

    /// <summary>
    /// An Excel 4.0 macro sheet.
    /// </summary>
    MacroSheet = 0x01,

    /// <summary>
    /// A chart sheet.
    /// </summary>
    Chart = 0x02,

    /// <summary>
    /// A Visual Basic module.
    /// </summary>
    VisualBasicModule = 0x06,
}
