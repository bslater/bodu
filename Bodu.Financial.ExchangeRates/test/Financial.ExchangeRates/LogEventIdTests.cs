// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LogEventIdTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using Bodu.Financial.ExchangeRates.Caching;
using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that the log events the exchange-rate packages declare carry distinct event ids, so a host that filters or
/// routes by event id can tell any two of them apart.
/// </summary>
[TestClass]
public class LogEventIdTests
{
    /// <summary>Selects every method a type declares, whatever its accessibility and whether static or not.</summary>
    private const BindingFlags DeclaredMethods =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    /// <summary>One public type from each exchange-rate package, naming the assemblies whose log events are collected.</summary>
    private static readonly Type[] PackageMarkers =
    [
        typeof(WebRateProvider),
        typeof(BoeRateProvider),
        typeof(EcbRateProvider),
        typeof(RbaRateProvider),
        typeof(ImfRateProvider),
        typeof(YahooRateProvider),
        typeof(OfxRateProvider),
        typeof(XeRateProvider),
        typeof(OandaRateProvider),
        typeof(FixerRateProvider),
        typeof(ExchangeRateHostRateProvider),
        typeof(FredRateProvider),
        typeof(CachingRateProvider),
        typeof(SqliteRateCache),
        typeof(DistributedRateCache),
    ];

    /// <summary>
    /// Verifies that no two log events declared across the exchange-rate packages share an event id.
    /// </summary>
    [TestMethod]
    public void EventId_WhenCollectedFromEveryProviderAssembly_ShouldBeUnique()
    {
        List<string> duplicates = CollectLogEvents()
            .GroupBy(entry => entry.EventId)
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(entry => entry.Name))}")
            .ToList();

        Assert.IsEmpty(duplicates, string.Join("; ", duplicates));
    }

    /// <summary>
    /// Verifies that the collection reads the events of every package known to log, so the uniqueness check cannot pass
    /// by finding nothing.
    /// </summary>
    [TestMethod]
    public void EventId_WhenCollectedFromEveryProviderAssembly_ShouldIncludeEachLoggingPackage()
    {
        HashSet<string> assemblies = CollectLogEvents().Select(entry => entry.Assembly).ToHashSet(StringComparer.Ordinal);

        string[] logging =
        [
            "Bodu.Financial.ExchangeRates",
            "Bodu.Financial.ExchangeRates.Boe",
            "Bodu.Financial.ExchangeRates.Ecb",
            "Bodu.Financial.ExchangeRates.Rba",
            "Bodu.Financial.ExchangeRates.Imf",
            "Bodu.Financial.ExchangeRates.Xe",
            "Bodu.Financial.ExchangeRates.Caching",
            "Bodu.Financial.ExchangeRates.Caching.Sqlite",
            "Bodu.Financial.ExchangeRates.Caching.Distributed",
        ];
        foreach (string name in logging)
            Assert.Contains(name, assemblies);
    }

    /// <summary>
    /// Collects every method that carries a <see cref="LoggerMessageAttribute" /> in the exchange-rate packages.
    /// </summary>
    /// <returns>Each log event's id, the assembly that declares it, and a label naming its method.</returns>
    private static IEnumerable<(int EventId, string Assembly, string Name)> CollectLogEvents()
    {
        foreach (Assembly assembly in PackageMarkers.Select(type => type.Assembly).Distinct())
        {
            string assemblyName = assembly.GetName().Name!;
            foreach (Type type in assembly.GetTypes())
            {
                foreach (MethodInfo method in type.GetMethods(DeclaredMethods))
                {
                    if (method.GetCustomAttribute<LoggerMessageAttribute>() is { } attribute)
                        yield return (attribute.EventId, assemblyName, $"{assemblyName}:{type.Name}.{method.Name}");
                }
            }
        }
    }
}
