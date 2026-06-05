// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateLocalizationExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides extension methods that apply an <see cref="INotableDateNameLocalizer" /> to resolved occurrences, replacing
/// each display name with its culture-specific form.
/// </summary>
public static class NotableDateLocalizationExtensions
{
    /// <summary>
    /// Returns the occurrence with its display name localized for the requested culture.
    /// </summary>
    /// <param name="notableDate">The resolved occurrence to localize.</param>
    /// <param name="localizer">The localizer that supplies the culture-specific name.</param>
    /// <param name="culture">The culture to localize for.</param>
    /// <returns>
    /// A copy with the localized display name, or the same occurrence when the localizer supplies no name.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="notableDate" />, <paramref name="localizer" />, or <paramref name="culture" /> is
    /// <see langword="null" />.
    /// </exception>
    public static NotableDate Localize(this NotableDate notableDate, INotableDateNameLocalizer localizer, CultureInfo culture)
    {
        ThrowHelper.ThrowIfNull(notableDate);
        ThrowHelper.ThrowIfNull(localizer);
        ThrowHelper.ThrowIfNull(culture);

        string? localized = localizer.GetDisplayName(notableDate, culture);
        return localized is null ? notableDate : notableDate with { DisplayName = localized };
    }

    /// <summary>
    /// Returns the occurrences with their display names localized for the requested culture.
    /// </summary>
    /// <param name="occurrences">The resolved occurrences to localize.</param>
    /// <param name="localizer">The localizer that supplies the culture-specific names.</param>
    /// <param name="culture">The culture to localize for.</param>
    /// <returns>The occurrences with localized display names, preserving order.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="occurrences" />, <paramref name="localizer" />, or <paramref name="culture" /> is
    /// <see langword="null" />.
    /// </exception>
    public static IReadOnlyList<NotableDate> Localize(this IReadOnlyList<NotableDate> occurrences, INotableDateNameLocalizer localizer, CultureInfo culture)
    {
        ThrowHelper.ThrowIfNull(occurrences);
        ThrowHelper.ThrowIfNull(localizer);
        ThrowHelper.ThrowIfNull(culture);

        NotableDate[] localized = new NotableDate[occurrences.Count];
        for (int i = 0; i < occurrences.Count; i++)
            localized[i] = occurrences[i].Localize(localizer, culture);

        return localized;
    }
}
