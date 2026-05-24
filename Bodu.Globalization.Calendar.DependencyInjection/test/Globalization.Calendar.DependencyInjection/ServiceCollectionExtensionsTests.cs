// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensionsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.DependencyInjection;

/// <summary>
/// Verifies the registration semantics, defaults, builder return values, and argument validation for the
/// <see cref="ServiceCollectionExtensions.AddNotableDates(IServiceCollection, Microsoft.Extensions.Configuration.IConfiguration?, string)" />
/// extension family.
/// </summary>
[TestClass]
public sealed partial class ServiceCollectionExtensionsTests
{
    /// <summary>
    /// A trivial in-memory <see cref="INotableDateRuleProvider" /> used to seed test compositions.
    /// </summary>
    private sealed class InMemoryRuleProvider : INotableDateRuleProvider
    {
        /// <summary>
        /// The rules surfaced from <see cref="LoadRules" />.
        /// </summary>
        private readonly IReadOnlyList<NotableDateRule> _rules;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryRuleProvider" /> class with the supplied rule set.
        /// </summary>
        /// <param name="rules">The rules to expose.</param>
        public InMemoryRuleProvider(params NotableDateRule[] rules) => _rules = rules;

        /// <inheritdoc />
        public IEnumerable<NotableDateRule> LoadRules() => _rules;
    }

    /// <summary>
    /// Builds a fixed-month rule used in test fixtures.
    /// </summary>
    /// <param name="name">The rule name.</param>
    /// <param name="month">The fixed month.</param>
    /// <param name="day">The fixed day.</param>
    /// <param name="territory">The optional territory scope.</param>
    /// <returns>A configured <see cref="NotableDateRule" />.</returns>
    private static NotableDateRule Fixed(string name, int month = 1, int day = 1, string? territory = null) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = month,
            Day = day,
            TerritoryCode = territory,
        };
}
