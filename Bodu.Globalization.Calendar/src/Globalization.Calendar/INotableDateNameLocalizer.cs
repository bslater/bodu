// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateNameLocalizer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Supplies a culture-specific display name for a resolved notable-date occurrence, supplementing the invariant
/// <see cref="NotableDate.DisplayName" /> carried by the concept.
/// </summary>
/// <remarks>
/// <para>
/// A localizer is applied on demand to resolved occurrences (for example through the
/// <c>Localize</c> extension methods), so resolution itself stays culture-agnostic and a caller can request any culture.
/// </para>
/// </remarks>
public interface INotableDateNameLocalizer
{
    /// <summary>
    /// Returns the display name of an occurrence for the requested culture.
    /// </summary>
    /// <param name="notableDate">The resolved occurrence whose name is being localized.</param>
    /// <param name="culture">The culture to localize for.</param>
    /// <returns>
    /// The localized display name, or <see langword="null" /> to fall back to the occurrence's existing display name.
    /// </returns>
    string? GetDisplayName(NotableDate notableDate, CultureInfo culture);
}
