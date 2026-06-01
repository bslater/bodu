// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeResolution.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Specifies the resolution level for a <see cref="System.DateTime" /> value.
/// </summary>
public enum DateTimeResolution
{
    /// <summary>
    /// Indicates resolution to a whole year.
    /// </summary>
    Year,

    /// <summary>
    /// Indicates resolution to a whole month.
    /// </summary>
    Month,

    /// <summary>
    /// Indicates resolution to a whole day.
    /// </summary>
    Day,

    /// <summary>
    /// Indicates resolution to a whole hour.
    /// </summary>
    Hour,

    /// <summary>
    /// Indicates resolution to a whole minute.
    /// </summary>
    Minute,

    /// <summary>
    /// Indicates resolution to a whole second.
    /// </summary>
    Second,

    /// <summary>
    /// Indicates resolution to a whole millisecond.
    /// </summary>
    Millisecond,

    /// <summary>
    /// Indicates resolution to a whole tick.
    /// </summary>
    Tick,
}