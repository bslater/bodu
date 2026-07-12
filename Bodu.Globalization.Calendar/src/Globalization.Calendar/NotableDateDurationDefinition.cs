// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDurationDefinition.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents how long a resolved notable-date occurrence lasts: either a fixed number of calendar days or a span whose
/// end date is independently calculated.
/// </summary>
/// <remarks>
/// <para>
/// A rule declares at most one duration definition. A <see langword="null" /> duration means the rule inherits the
/// parent concept's <see cref="NotableDateDefinition.DefaultDurationDays" />. The two concrete kinds —
/// <see cref="FixedDurationDefinition" /> and <see cref="CalculatedEndDateDurationDefinition" /> — are mutually
/// exclusive by construction, so a rule can never declare both a fixed day count and a calculated end date.
/// </para>
/// </remarks>
/// <seealso cref="FixedDurationDefinition" />
/// <seealso cref="CalculatedEndDateDurationDefinition" />
public abstract record NotableDateDurationDefinition;
