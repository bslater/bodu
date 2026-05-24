// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

/// <summary>
/// Provides a set of <see langword="static" /> ( <see langword="Shared" /> in Visual Basic) methods that extend the
/// <see cref="System.DateTime" /> structure with calculations that delegate to an <see cref="INotableDateService" />,
/// including working-day arithmetic, non-working-day predicates, range counting, and notable-date queries.
/// </summary>
/// <remarks>
/// <para>
/// Every member is offered as two overloads: one that accepts an explicit <see cref="INotableDateService" /> parameter
/// and one that reads the ambient <see cref="NotableDateContext.Default" /> service. Hosts using dependency injection
/// should prefer the explicit overloads; convenience-oriented call sites should use the ambient ones.
/// </para>
/// <para>
/// Day-stepping inside walk and counting members is performed in Gregorian ticks. The <c>calendarType</c> argument is
/// forwarded to the service for rule resolution and is not used to drive day arithmetic; the configured calendar
/// handlers on the service therefore determine which rules contribute to working-day classification.
/// </para>
/// <para>
/// Returned <see cref="DateTime" /> instances preserve the <see cref="DateTime.Kind" /> of the input value. Walk
/// methods that would advance past <see cref="DateTime.MinValue" /> or <see cref="DateTime.MaxValue" /> throw
/// <see cref="ArgumentOutOfRangeException" />.
/// </para>
/// </remarks>
public static partial class NotableDateTimeExtensions
{
}
