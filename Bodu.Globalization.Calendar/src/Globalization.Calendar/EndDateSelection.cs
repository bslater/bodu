// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EndDateSelection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Specifies how a calculated end-date anchor is selected relative to the occurrence's start anchor.
/// </summary>
/// <remarks>
/// The end strategy is evaluated for the start anchor's civil year and, when no acceptable candidate exists, the
/// following civil year; the earliest candidate satisfying this rule is chosen. This lets a December start strategy and
/// a January end strategy form one cross-year occurrence without an explicit year offset.
/// </remarks>
/// <seealso cref="CalculatedEndDateDurationDefinition" />
public enum EndDateSelection
{
    /// <summary>
    /// Selects the earliest end anchor on or after the start anchor.
    /// </summary>
    FirstOnOrAfterStart = 0,

    /// <summary>
    /// Selects the earliest end anchor strictly after the start anchor.
    /// </summary>
    FirstAfterStart,
}
