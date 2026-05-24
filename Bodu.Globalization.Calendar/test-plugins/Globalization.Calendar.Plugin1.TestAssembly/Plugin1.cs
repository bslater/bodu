// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Plugin1.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using Bodu.Globalization.Calendar.Plugins;

[assembly: NotableDatePlugin(typeof(Bodu.Globalization.Calendar.Plugin1.TestAssembly.HarnessPlugin))]
namespace Bodu.Globalization.Calendar.Plugin1.TestAssembly;

/// <summary>
/// Test-only plugin that exercises the happy-path through <see cref="ExternalPluginLoader" />: it carries the required
/// attribute, implements both <see cref="INotableDateRulePlugin" /> and <see cref="INotableDateAlgorithmPlugin" />, and
/// returns deterministic fixtures for tests to assert against.
/// </summary>
public sealed class HarnessPlugin
    : INotableDateRulePlugin
    , INotableDateAlgorithmPlugin
{
    public const string PluginName = "Bodu.Test.Harness.Plugin1";
    public const string RuleName = "Harness Test Day";
    public const string AlgorithmKey = "harness.static";
    public static readonly DateTime FixedAlgorithmDate = new(2027, 6, 15);

    public string Name => PluginName;

    public Version Version { get; } = new(1, 0, 0);

    public IEnumerable<INotableDateRuleProvider> GetRuleProviders()
    {
        yield return new StaticRuleProvider();
    }

    public IEnumerable<KeyValuePair<string, INotableDateAlgorithm>> GetAlgorithms()
    {
        yield return new KeyValuePair<string, INotableDateAlgorithm>(AlgorithmKey, new StaticAlgorithm());
    }

    private sealed class StaticRuleProvider
        : INotableDateRuleProvider
    {
        public IEnumerable<NotableDateRule> LoadRules()
        {
            yield return new NotableDateRule
            {
                Name = RuleName,
                Strategy = DateResolutionStrategy.Fixed,
                Category = NotableDateCategory.Observance,
                Month = 6,
                Day = 15,
                TerritoryCode = "ZZ",
                Tags = ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "Harness"),
            };
        }
    }

    private sealed class StaticAlgorithm
        : INotableDateAlgorithm
    {
        public DateTime? GetDate(int year, System.Globalization.Calendar? calendar = null) => FixedAlgorithmDate;
    }
}
