// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

/// <summary>
/// Contains unit tests for the <see cref="NotableDateTimeExtensions" /> extension methods.
/// </summary>
[TestClass]
public partial class NotableDateTimeExtensionsTests
{
    /// <summary>
    /// Builds a deterministic <see cref="NotableDateService" /> from the supplied in-memory rules using a Saturday/Sunday weekend
    /// definition. The service does not load any embedded resources, ensuring that tests observe only the rules declared at the call site.
    /// </summary>
    /// <param name="rules">The rules to expose through the service.</param>
    /// <returns>A configured <see cref="NotableDateService" /> instance.</returns>
    private static NotableDateService BuildService(params NotableDateRule[] rules) =>
        new(new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(rules) }, CalendarWeekendDefinition.SaturdaySunday);

    /// <summary>
    /// Constructs a <see cref="NotableDateRule" /> for a fixed Gregorian month/day, optionally non-working and territory-scoped.
    /// </summary>
    /// <param name="name">The canonical name of the rule.</param>
    /// <param name="month">The month component.</param>
    /// <param name="day">The day component.</param>
    /// <param name="category">The category to assign. Defaults to <see cref="NotableDateCategory.Holiday" />.</param>
    /// <param name="nonWorking">When <see langword="true" /> the resolved date is treated as a non-working day.</param>
    /// <param name="territory">Optional territory restriction.</param>
    /// <returns>A new <see cref="NotableDateRule" /> ready for use with an <see cref="InMemoryRuleProvider" />.</returns>
    private static NotableDateRule Fixed(string name, int month, int day, NotableDateCategory category = NotableDateCategory.Holiday, bool nonWorking = false, string? territory = null) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Fixed,
            Category = category,
            Month = month,
            Day = day,
            IsNonWorkingDay = nonWorking,
            TerritoryCode = territory,
        };

    /// <summary>
    /// Provides an in-memory implementation of <see cref="INotableDateRuleProvider" /> for tests, returning a fixed array of rules
    /// without any persistence or resource loading.
    /// </summary>
    private sealed class InMemoryRuleProvider
        : INotableDateRuleProvider
    {
        /// <summary>The rules surfaced by <see cref="LoadRules" />.</summary>
        private readonly IEnumerable<NotableDateRule> _rules;

        /// <summary>
        /// Initialises a new <see cref="InMemoryRuleProvider" /> with the supplied rules.
        /// </summary>
        /// <param name="rules">The rules to surface.</param>
        public InMemoryRuleProvider(params NotableDateRule[] rules) => _rules = rules;

        /// <inheritdoc />
        public IEnumerable<NotableDateRule> LoadRules() => _rules;
    }
}
