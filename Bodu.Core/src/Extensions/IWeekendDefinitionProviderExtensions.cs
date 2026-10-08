// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IWeekendDefinitionProviderExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Provides conversions from <see cref="IWeekendDefinitionProvider" /> to the canonical <see cref="DayOfWeekSet" />
/// value type used by the working-week APIs.
/// </summary>
public static class IWeekendDefinitionProviderExtensions
{
    /// <summary>
    /// Returns the working week an <see cref="IWeekendDefinitionProvider" /> implies: the <see cref="DayOfWeekSet" />
    /// of the days that are not weekend days.
    /// </summary>
    /// <param name="provider">The provider whose weekend the working week complements.</param>
    /// <returns>The days <paramref name="provider" /> does not count as weekend days.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The provider is asked about each of the seven days once. Use the result with APIs that take a working week as a
    /// <see cref="DayOfWeekSet" />, such as <see cref="DateOnlyExtensions.IsInWorkingWeek(DateOnly, DayOfWeekSet)" />;
    /// they test one bit per day and never consult the provider again.
    /// </para>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// IWeekendDefinitionProvider provider = new FridaySaturdayWeekend();
    /// DayOfWeekSet workingWeek = provider.ToWorkingWeek(); // Sunday to Thursday
    /// bool working = new DateOnly(2026, 10, 8).IsInWorkingWeek(workingWeek); // true, a Thursday
    ///]]>
    /// </code>
    /// </example>
    /// </remarks>
    public static DayOfWeekSet ToWorkingWeek(this IWeekendDefinitionProvider provider)
    {
        ThrowHelper.ThrowIfNull(provider);

        DayOfWeekSet workingWeek = DayOfWeekSet.Empty;
        for (int i = 0; i < 7; i++)
        {
            var day = (DayOfWeek)i;
            if (!provider.IsWeekend(day))
                workingWeek = workingWeek.With(day);
        }

        return workingWeek;
    }
}
