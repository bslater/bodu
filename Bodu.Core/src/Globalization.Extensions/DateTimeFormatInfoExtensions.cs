// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeFormatInfoExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Extensions;

/// <summary>
/// Provides culture-aware calendar lookups against <see cref="System.Globalization.DateTimeFormatInfo" /> that derive
/// week-structure values which the BCL exposes only indirectly.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="System.Globalization.DateTimeFormatInfo" /> publishes the first day of the week through
/// <see cref="System.Globalization.DateTimeFormatInfo.FirstDayOfWeek" />, but offers no symmetrical accessor for the
/// last day of the week — yet that value is needed for week-end snapping, weekend predicates, and report headers in any
/// locale-driven calendar code. This class supplies the missing accessors, derived from the same culture data so the
/// result matches whatever the active culture says is the start of the week.
/// </para>
/// <para>
/// The API surface is intentionally small and is shaped around culture-driven derivations rather than free-form
/// formatting: the central helper is <c>LastDayOfWeek</c>, which returns the <see cref="DayOfWeek" /> immediately
/// preceding <see cref="System.Globalization.DateTimeFormatInfo.FirstDayOfWeek" /> in the cyclic week.
/// </para>
/// <para>
/// All methods are pure, allocation-free, and reject a <see langword="null" />
/// <see cref="System.Globalization.DateTimeFormatInfo" /> with <see cref="ArgumentNullException" />. Results are
/// sensitive to the supplied <see cref="System.Globalization.DateTimeFormatInfo" /> — they intentionally do <em>not
/// </em> consult ambient culture state — so callers can rely on deterministic behavior even when the current thread's
/// culture changes between calls.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var enGb = CultureInfo.GetCultureInfo("en-GB").DateTimeFormat;
/// var enUs = CultureInfo.GetCultureInfo("en-US").DateTimeFormat;
///
/// DayOfWeek lastInBritain = enGb.LastDayOfWeek();
/// // => DayOfWeek.Sunday (en-GB starts on Monday, so the week ends on Sunday)
///
/// DayOfWeek lastInAmerica = enUs.LastDayOfWeek();
/// // => DayOfWeek.Saturday (en-US starts on Sunday, so the week ends on Saturday)
///]]>
/// </code>
/// </example>
/// </remarks>
public static partial class DateTimeFormatInfoExtensions
{ }