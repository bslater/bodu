// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedDurationDefinition.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents a fixed occurrence duration expressed as a number of calendar days, inclusive of the occurrence start
/// date.
/// </summary>
/// <param name="Days">The number of calendar days the occurrence spans, inclusive of the first day.</param>
/// <remarks>
/// A fixed duration of one day is a single-day occurrence. The resolved end date is
/// <c>Date + <see cref="Days" /> - 1</c>. Values below one are clamped to one at resolution time.
/// </remarks>
/// <seealso cref="NotableDateDurationDefinition" />
public sealed record FixedDurationDefinition(int Days)
    : NotableDateDurationDefinition;
