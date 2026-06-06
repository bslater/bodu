// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateNameLocalizer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// A dictionary-backed <see cref="INotableDateNameLocalizer" /> keyed by notable-date concept identifier and culture,
/// with parent-culture fallback.
/// </summary>
/// <remarks>
/// <para>
/// Names are registered against a concept id and a culture; a lookup walks the culture's fallback chain (for example
/// <c>fr-FR</c> → <c>fr</c> → invariant), returning the first registered name. Registering against
/// <see cref="CultureInfo.InvariantCulture" /> supplies a default that applies when no more specific culture matches.
/// </para>
/// </remarks>
public sealed class NotableDateNameLocalizer : INotableDateNameLocalizer
{
    /// <summary>
    /// The registered display names keyed by concept id and culture name.
    /// </summary>
    private readonly Dictionary<(string Id, string Culture), string> _names = new();

    /// <summary>
    /// Registers a display name for a concept and culture, replacing any existing registration.
    /// </summary>
    /// <param name="notableDateId">The notable-date concept identifier the name applies to.</param>
    /// <param name="culture">The culture the name is expressed in.</param>
    /// <param name="displayName">The localized display name.</param>
    /// <returns>The same localizer, to allow chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="notableDateId" />, <paramref name="culture" />, or <paramref name="displayName" /> is
    /// <see langword="null" />.
    /// </exception>
    public NotableDateNameLocalizer Register(string notableDateId, CultureInfo culture, string displayName)
    {
        ThrowHelper.ThrowIfNull(notableDateId);
        ThrowHelper.ThrowIfNull(culture);
        ThrowHelper.ThrowIfNull(displayName);

        _names[(notableDateId, culture.Name)] = displayName;
        return this;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">
    /// <paramref name="notableDate" /> or <paramref name="culture" /> is <see langword="null" />.
    /// </exception>
    public string? GetDisplayName(NotableDate notableDate, CultureInfo culture)
    {
        ThrowHelper.ThrowIfNull(notableDate);
        ThrowHelper.ThrowIfNull(culture);

        CultureInfo current = culture;
        while (true)
        {
            if (_names.TryGetValue((notableDate.NotableDateId, current.Name), out var name))
                return name;

            // The invariant culture (empty name) is the parent of its own parent and the end of every fallback chain.
            if (current.Name.Length == 0)
                return null;

            current = current.Parent;
        }
    }
}
