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
    /// Converts an <see cref="IWeekendDefinitionProvider" /> into the canonical <see cref="DayOfWeekSet" /> whose
    /// selected days are the complement of the provider's weekend.
    /// </summary>
    /// <param name="provider">
    /// The provider whose weekend semantics are projected onto a <see cref="DayOfWeekSet" />.
    /// </param>
    /// <returns>The working-week <see cref="DayOfWeekSet" /> implied by <paramref name="provider" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Use this helper when adapting an <see cref="IWeekendDefinitionProvider" /> implementation to APIs that accept a
    /// <see cref="DayOfWeekSet" /> directly - for example, the <see cref="DayOfWeekSet" />-accepting
    /// <c>NotableDateService</c> constructor.
    /// </para>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// IWeekendDefinitionProvider provider = new MyCustomWeekend();
    /// DayOfWeekSet workingWeek = provider.ToDayOfWeekSet();
    /// var service = new NotableDateService(ruleProviders, workingWeek);
    ///]]>
    /// </code>
    /// </example>
    /// </remarks>
    public static DayOfWeekSet ToDayOfWeekSet(this IWeekendDefinitionProvider provider)
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
