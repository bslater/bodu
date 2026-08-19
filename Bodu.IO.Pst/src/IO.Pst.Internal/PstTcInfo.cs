// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstTcInfo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Represents a parsed <c>TCINFO</c>: the table context's row geometry, its row-index and row-matrix references, and
/// its column descriptors.
/// </summary>
/// <param name="EndOffset4">The ending offset of the row's 8- and 4-byte cell region (<c>rgib[TCI_4b]</c>).</param>
/// <param name="EndOffset2">The ending offset of the row's 2-byte cell region (<c>rgib[TCI_2b]</c>).</param>
/// <param name="EndOffset1">The ending offset of the row's 1-byte cell region (<c>rgib[TCI_1b]</c>).</param>
/// <param name="RowWidth">The full row width including the existence bitmap (<c>rgib[TCI_bm]</c>).</param>
/// <param name="RowIndexHid">The <c>HID</c> of the row-index BTree-on-heap.</param>
/// <param name="RowsHnid">The <c>HNID</c> of the row matrix; zero when the table has no rows.</param>
/// <param name="Columns">The column descriptors in stored order.</param>
internal readonly record struct PstTcInfo(
    ushort EndOffset4,
    ushort EndOffset2,
    ushort EndOffset1,
    ushort RowWidth,
    uint RowIndexHid,
    uint RowsHnid,
    PstTcColumn[] Columns);
