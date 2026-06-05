// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDefinition.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents a single notable-date concept: its stable identity, presentation metadata, inherited defaults, and one or
/// more calculation rules.
/// </summary>
/// <remarks>
/// <para>
/// A concept owns multiple rules so that territory-specific variants — for example a national and a regional rule that
/// merely share a display name — coexist as distinct, independently addressable rules rather than collapsing.
/// </para>
/// </remarks>
public sealed class NotableDateDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateDefinition" /> class.
    /// </summary>
    /// <param name="id">The stable identifier of the concept.</param>
    /// <param name="displayName">The human-readable name of the concept.</param>
    /// <param name="category">The default category inherited by child rules.</param>
    /// <param name="defaultNonWorkingDay">The default non-working-day flag inherited by child rules.</param>
    /// <param name="defaultDurationDays">The default duration in days inherited by child rules.</param>
    /// <param name="tags">The tags inherited by child rules unless overridden.</param>
    /// <param name="rules">The calculation rules belonging to the concept.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="id" />, <paramref name="displayName" />, <paramref name="tags" />, or <paramref name="rules" />
    /// is <see langword="null" />.
    /// </exception>
    public NotableDateDefinition(
        string id,
        string displayName,
        NotableDateCategory category,
        bool defaultNonWorkingDay,
        int defaultDurationDays,
        IEnumerable<string> tags,
        IEnumerable<NotableDateRule> rules)
    {
        ThrowHelper.ThrowIfNull(id);
        ThrowHelper.ThrowIfNull(displayName);
        ThrowHelper.ThrowIfNull(tags);
        ThrowHelper.ThrowIfNull(rules);

        this.Id = id;
        this.DisplayName = displayName;
        this.Category = category;
        this.DefaultNonWorkingDay = defaultNonWorkingDay;
        this.DefaultDurationDays = defaultDurationDays;
        this.Tags = tags.ToArray();
        this.Rules = rules.ToArray();
    }

    /// <summary>
    /// Gets the stable identifier of the concept.
    /// </summary>
    /// <returns>The concept id.</returns>
    public string Id { get; }

    /// <summary>
    /// Gets the human-readable name of the concept.
    /// </summary>
    /// <returns>The display name.</returns>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the default category inherited by child rules.
    /// </summary>
    /// <returns>The concept's <see cref="NotableDateCategory" />.</returns>
    public NotableDateCategory Category { get; }

    /// <summary>
    /// Gets a value indicating whether child rules default to a non-working day.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when child rules default to a non-working day; otherwise <see langword="false" />.
    /// </returns>
    public bool DefaultNonWorkingDay { get; }

    /// <summary>
    /// Gets the default duration in days inherited by child rules.
    /// </summary>
    /// <returns>The default duration.</returns>
    public int DefaultDurationDays { get; }

    /// <summary>
    /// Gets the tags inherited by child rules unless overridden.
    /// </summary>
    /// <returns>The concept tags; empty when none are declared.</returns>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets the calculation rules belonging to the concept.
    /// </summary>
    /// <returns>The rules; may be empty after override removal.</returns>
    public IReadOnlyList<NotableDateRule> Rules { get; }

    /// <summary>
    /// Creates a copy of the concept with its rules replaced.
    /// </summary>
    /// <param name="rules">The replacement rules.</param>
    /// <returns>A new <see cref="NotableDateDefinition" /> with identical metadata and the supplied rules.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rules" /> is <see langword="null" />.</exception>
    public NotableDateDefinition WithRules(IEnumerable<NotableDateRule> rules)
    {
        ThrowHelper.ThrowIfNull(rules);

        return new NotableDateDefinition(
            this.Id,
            this.DisplayName,
            this.Category,
            this.DefaultNonWorkingDay,
            this.DefaultDurationDays,
            this.Tags,
            rules);
    }
}
