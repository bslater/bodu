// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffSubstreamType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Identifies the kind of substream a beginning-of-file record opens.
/// </summary>
/// <remarks>
/// A BIFF workbook stream is a sequence of substreams, each bracketed by a <c>BOF</c> and an <c>EOF</c> record. The
/// first is the workbook globals; each sheet that follows is its own substream. Values match the <c>dt</c> field of the
/// <c>BOF</c> record.
/// </remarks>
public enum BiffSubstreamType : ushort
{
    /// <summary>
    /// The workbook globals substream: the bound-sheet directory, the shared string table, fonts, formats, and styles.
    /// </summary>
    WorkbookGlobals = 0x0005,

    /// <summary>
    /// A Visual Basic module substream.
    /// </summary>
    VisualBasicModule = 0x0006,

    /// <summary>
    /// A worksheet (or dialog sheet) substream.
    /// </summary>
    Worksheet = 0x0010,

    /// <summary>
    /// A chart substream.
    /// </summary>
    Chart = 0x0020,

    /// <summary>
    /// An Excel 4.0 macro sheet substream.
    /// </summary>
    MacroSheet = 0x0040,

    /// <summary>
    /// A workspace file substream.
    /// </summary>
    Workspace = 0x0100,
}
