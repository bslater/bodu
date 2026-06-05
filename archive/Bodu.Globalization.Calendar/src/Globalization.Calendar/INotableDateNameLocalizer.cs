// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateNameLocalizer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Translates a canonical notable date name into the active culture's display form.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="NotableDateRule.Name" /> is authored in invariant English. Implementations of
/// <see cref="INotableDateNameLocalizer" /> resolve that key to a culture-specific string — typically by looking it up
/// in a <c>.resx</c> file or external translation catalogue.
/// </para>
/// <para>
/// Supply an implementation via the <c>nameLocalizer</c> parameter of the <see cref="NotableDateService" />
/// constructor. When supplied, the service replaces each resolved <see cref="NotableDate.Name" /> with the localized
/// form before returning results.
/// </para>
/// </remarks>
/// <example>
/// <para>
/// A <c>.resx</c>-backed implementation that falls back to the canonical English name:
/// </para>
/// <code>
///<![CDATA[
/// public sealed class ResxNameLocalizer : INotableDateNameLocalizer
/// {
///     public string GetDisplayName(NotableDate notableDate, CultureInfo? culture = null)
///     {
///         string? translation = HolidayNames.ResourceManager
///             .GetString(notableDate.Name, culture ?? CultureInfo.CurrentCulture);
///
///         return translation ?? notableDate.Name;
///     }
/// }
///]]>
/// </code>
/// </example>
public interface INotableDateNameLocalizer
{
    /// <summary>
    /// Returns the display name for the supplied notable date in the requested culture.
    /// </summary>
    /// <param name="notableDate">The notable date being rendered. Must not be <see langword="null" />.</param>
    /// <param name="culture">
    /// The target culture. <see langword="null" /> defaults to <see cref="CultureInfo.CurrentCulture" />.
    /// </param>
    /// <returns>
    /// The localized display name. Implementations should fall back to <see cref="NotableDate.Name" /> when no
    /// translation is found.
    /// </returns>
    string GetDisplayName(NotableDate notableDate, CultureInfo? culture = null);
}
