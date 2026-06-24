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
/// <seealso cref="NotableDateRule" /> <seealso href="../guides/calendar/rule-reference.html">NotableDateRule and
/// adjustment-policy reference (guide)</seealso>
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

        Id = id;
        DisplayName = displayName;
        Category = category;
        DefaultNonWorkingDay = defaultNonWorkingDay;
        DefaultDurationDays = defaultDurationDays;
        Tags = [.. tags];
        Rules = [.. rules];
    }

    /// <summary>
    /// Gets the stable identifier of the concept.
    /// </summary>
    /// <value>The concept id.</value>
    public string Id { get; }

    /// <summary>
    /// Gets the human-readable name of the concept.
    /// </summary>
    /// <value>The display name.</value>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the default category inherited by child rules.
    /// </summary>
    /// <value>The concept's <see cref="NotableDateCategory" />.</value>
    public NotableDateCategory Category { get; }

    /// <summary>
    /// Gets a value indicating whether child rules default to a non-working day.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when child rules default to a non-working day; otherwise <see langword="false" />.
    /// </value>
    public bool DefaultNonWorkingDay { get; }

    /// <summary>
    /// Gets the default duration in days inherited by child rules.
    /// </summary>
    /// <value>The default duration.</value>
    public int DefaultDurationDays { get; }

    /// <summary>
    /// Gets the tags inherited by child rules unless overridden.
    /// </summary>
    /// <value>The concept tags; empty when none are declared.</value>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Gets the calculation rules belonging to the concept.
    /// </summary>
    /// <value>The rules; may be empty after override removal.</value>
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
            Id,
            DisplayName,
            Category,
            DefaultNonWorkingDay,
            DefaultDurationDays,
            Tags,
            rules);
    }
}
