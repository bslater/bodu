// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaTestCellKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Identifies what a cell of a synthetic F11.1 workbook holds, and so which record <see cref="RbaTestWorkbook" />
/// writes for it.
/// </summary>
internal enum RbaTestCellKind
{
    /// <summary>The cell has no record at all.</summary>
    Absent,

    /// <summary>The cell is a formatted blank: a <c>BLANK</c> record, or one cell of a <c>MULBLANK</c> run.</summary>
    Blank,

    /// <summary>
    /// The cell holds text: a <c>LABELSST</c> record, or a <c>FORMULA</c> record whose cached result a <c>STRING</c>
    /// record carries.
    /// </summary>
    Text,

    /// <summary>
    /// The cell holds a number: an <c>RK</c> record or one cell of a <c>MULRK</c> run when the number has an exact RK
    /// form, and a <c>NUMBER</c> record otherwise.
    /// </summary>
    Number,
}
