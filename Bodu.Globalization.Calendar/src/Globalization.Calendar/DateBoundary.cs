// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateBoundary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Specifies whether a boundary anchor date is itself part of a calculated occurrence span.
/// </summary>
/// <seealso cref="CalculatedEndDateDurationDefinition" />
public enum DateBoundary
{
    /// <summary>
    /// The boundary anchor is part of the span: the effective start is the start anchor, and the effective end is the
    /// end anchor.
    /// </summary>
    Inclusive = 0,

    /// <summary>
    /// The boundary anchor is excluded from the span: the effective start is the day after the start anchor, and the
    /// effective end is the day before the end anchor.
    /// </summary>
    Exclusive,
}
