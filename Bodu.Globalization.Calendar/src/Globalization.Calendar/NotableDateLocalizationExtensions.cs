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
/// <remarks>
/// <para>
/// Resolution always emits the authored (invariant) display name; localization is a separate, opt-in step applied to
/// the results. When the localizer supplies no name for an occurrence in the requested culture, that occurrence is
/// returned unchanged so the invariant name remains as a fallback.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// IReadOnlyList<NotableDate> dates = service.Resolve(2026, "CA-QC");
///
/// // Replace display names with their French-Canadian forms where the localizer has them.
/// INotableDateNameLocalizer localizer = new NotableDateNameLocalizer(translations);
/// IReadOnlyList<NotableDate> localized = dates.Localize(localizer, CultureInfo.GetCultureInfo("fr-CA"));
///]]>
/// </code>
/// </example>
/// <seealso cref="INotableDateNameLocalizer" /> <seealso cref="NotableDate" />
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

        var localized = localizer.GetDisplayName(notableDate, culture);
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

        var localized = new NotableDate[occurrences.Count];
        for (var i = 0; i < occurrences.Count; i++)
            localized[i] = occurrences[i].Localize(localizer, culture);

        return localized;
    }
}
