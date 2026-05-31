// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceBuilderExtensionsTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Globalization.Calendar.DependencyInjection;

/// <summary>
/// Verifies the registration semantics of every extension method declared on
/// <see cref="NotableDateServiceBuilderExtensions" />, including argument validation, accumulation, and effective
/// surfacing on the resolved <see cref="INotableDateService" />.
/// </summary>
[TestClass]
public sealed partial class NotableDateServiceBuilderExtensionsTests
{
    /// <summary>
    /// A trivial in-memory <see cref="INotableDateRuleProvider" /> used to compose isolated tests.
    /// </summary>
    private sealed class InMemoryRuleProvider : INotableDateRuleProvider
    {
        /// <summary>
        /// The rules surfaced from <see cref="LoadRules" />.
        /// </summary>
        private readonly IReadOnlyList<NotableDateRule> _rules;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryRuleProvider" /> class.
        /// </summary>
        /// <param name="rules">The rules to expose.</param>
        public InMemoryRuleProvider(params NotableDateRule[] rules) => _rules = rules;

        /// <inheritdoc />
        public IEnumerable<NotableDateRule> LoadRules() => _rules;
    }

    /// <summary>
    /// A trivial <see cref="INotableDateRuleOverrideProvider" /> used to verify additions surface through DI.
    /// </summary>
    private sealed class StaticOverrideProvider : INotableDateRuleOverrideProvider
    {
        /// <summary>
        /// The additions surfaced from <see cref="GetAdditions" />.
        /// </summary>
        private readonly IReadOnlyList<NotableDateRule> _additions;

        /// <summary>
        /// The removals surfaced from <see cref="GetRemovals" />.
        /// </summary>
        private readonly IReadOnlyList<RuleRemoval> _removals;

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticOverrideProvider" /> class.
        /// </summary>
        /// <param name="additions">The additions to expose.</param>
        /// <param name="removals">The removals to expose.</param>
        public StaticOverrideProvider(IReadOnlyList<NotableDateRule> additions, IReadOnlyList<RuleRemoval> removals)
        {
            _additions = additions;
            _removals = removals;
        }

        /// <inheritdoc />
        public IEnumerable<NotableDateRule> GetAdditions() => _additions;

        /// <inheritdoc />
        public IEnumerable<RuleRemoval> GetRemovals() => _removals;
    }

    /// <summary>
    /// Builds a fixed-month rule used in tests.
    /// </summary>
    /// <param name="name">The rule name.</param>
    /// <param name="month">The fixed month.</param>
    /// <param name="day">The fixed day.</param>
    /// <returns>A configured <see cref="NotableDateRule" />.</returns>
    private static NotableDateRule Fixed(string name, int month = 1, int day = 1) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = month,
            Day = day,
        };

    /// <summary>
    /// Builds a fresh <see cref="INotableDateServiceBuilder" /> over an empty service collection.
    /// </summary>
    /// <returns>The builder.</returns>
    private static (IServiceCollection Services, INotableDateServiceBuilder Builder) NewBuilder()
    {
        ServiceCollection services = new();
        INotableDateServiceBuilder builder = services.AddNotableDates();
        return (services, builder);
    }
}
