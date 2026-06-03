// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CookbookOverride.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Serves as the base for an ID-targeted override operation parsed from a cookbook's <c>Overrides</c> section.
/// </summary>
/// <remarks>
/// <para>
/// Overrides target stable identities. An operation that targets one rule never affects its sibling rules, even when
/// they share a display name.
/// </para>
/// </remarks>
internal abstract class CookbookOverride
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CookbookOverride" /> class.
    /// </summary>
    /// <param name="notableDateRef">The identifier of the targeted notable-date concept.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="notableDateRef" /> is <see langword="null" />.
    /// </exception>
    protected CookbookOverride(string notableDateRef)
    {
        ThrowHelper.ThrowIfNull(notableDateRef);

        this.NotableDateRef = notableDateRef;
    }

    /// <summary>
    /// Gets the identifier of the targeted notable-date concept.
    /// </summary>
    /// <returns>The targeted notable-date id.</returns>
    public string NotableDateRef { get; }
}
