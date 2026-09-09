// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffSheetType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Identifies the kind of sheet a <c>BOUNDSHEET</c> record describes.
/// </summary>
/// <remarks>
/// Values match the <c>dt</c> byte of a <c>BOUNDSHEET</c> record. The byte is exposed unmasked, so a kind the
/// enumeration does not name is preserved as its raw value rather than rejected. Only a <see cref="Worksheet" />
/// carries the cell records the codec's cell accessors decode.
/// </remarks>
/// <seealso cref="BiffBoundSheetRecord.SheetType" /> <seealso cref="BiffSubstreamType" />
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
