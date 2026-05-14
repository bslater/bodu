// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

/// <summary>
/// Provides a set of <see langword="static" /> ( <see langword="Shared" /> in Visual Basic) methods that extend the
/// <see cref="System.DateOnly" /> structure with calculations that delegate to an <see cref="INotableDateService" />, including
/// working-day arithmetic, non-working-day predicates, range counting, and notable-date queries.
/// </summary>
/// <remarks>
/// <para>
/// Every member is offered as two overloads: one that accepts an explicit <see cref="INotableDateService" /> parameter and one that
/// reads the ambient <see cref="NotableDateContext.Default" /> service. Hosts using dependency injection should prefer the explicit
/// overloads; convenience-oriented call sites should use the ambient ones.
/// </para>
/// <para>
/// Day-stepping inside walk and counting members is performed in calendar-day units. <see cref="DateOnly" /> values are bridged to
/// <see cref="DateTime" /> at <see cref="System.TimeOnly.MinValue" /> when calling into the service so that rule-resolution semantics
/// match those of the <c>NotableDateTimeExtensions</c> overloads. Time-of-day information is therefore ignored.
/// </para>
/// <para>
/// Walk methods that would advance past <see cref="DateOnly.MinValue" /> or <see cref="DateOnly.MaxValue" /> throw
/// <see cref="ArgumentOutOfRangeException" />.
/// </para>
/// </remarks>
public static partial class NotableDateOnlyExtensions
{
}
