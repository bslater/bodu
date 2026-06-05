// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PriorityDirection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Determines whether a higher or lower numeric priority value takes precedence during selection.
/// </summary>
/// <remarks>
/// <para>
/// Priority has a single meaning across rule selection, adjustment selection, override application, and collision
/// resolution. The recommended default is <see cref="HigherWins" />.
/// </para>
/// </remarks>
public enum PriorityDirection
{
    /// <summary>
    /// A higher numeric priority value wins.
    /// </summary>
    HigherWins = 0,

    /// <summary>
    /// A lower numeric priority value wins.
    /// </summary>
    LowerWins,
}
