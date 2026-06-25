// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyCatalogueTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

/// <summary>
/// Verifies the source-generated <see cref="ICurrency" /> tag-type catalogue under
/// <c>Bodu.Financial.Currencies</c>: every shipped currency exposes self-consistent metadata that matches its
/// <see cref="CurrencyRegistry" /> entry, and every tag type follows the non-instantiable sealed design.
/// </summary>
[TestClass]
public class CurrencyCatalogueTests
{
    /// <summary>
    /// Enumerates every shipped <see cref="ICurrency" /> tag type in the catalogue assembly.
    /// </summary>
    /// <returns>The sealed currency tag types declared under <c>Bodu.Financial.Currencies</c>.</returns>
    private static IEnumerable<Type> CurrencyTagTypes() =>
        typeof(ICurrency).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsSealed: true, Namespace: "Bodu.Financial.Currencies" }
                && typeof(ICurrency).IsAssignableFrom(t));

    /// <summary>
    /// Reads a declared <see langword="static" /> property by name, or returns <see langword="null" /> when the tag
    /// type relies on the interface default rather than declaring the member.
    /// </summary>
    /// <param name="type">The tag type to inspect.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The property value, or <see langword="null" /> when the property is not declared.</returns>
    private static object? ReadStatic(Type type, string name) =>
        type.GetProperty(name, BindingFlags.Public | BindingFlags.Static)?.GetValue(null);

    /// <summary>
    /// Verifies that every shipped currency tag type exposes metadata that is internally valid and consistent with the
    /// runtime <see cref="CurrencyRegistry" /> entry produced by the same generator.
    /// </summary>
    [TestMethod]
    public void CurrencyCatalogue_EveryShippedTagType_ExposesMetadataConsistentWithRegistry()
    {
        var types = CurrencyTagTypes().ToList();

        Assert.IsGreaterThanOrEqualTo(180, types.Count, $"Expected the full ISO 4217 catalogue; found only {types.Count} tag types.");

        foreach (Type type in types)
        {
            // Exercise every declared static getter so the generated metadata members are all evaluated.
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
                _ = property.GetValue(null);

            string isoCode = (string)ReadStatic(type, nameof(ICurrency.IsoCode))!;
            int minorUnits = (int)ReadStatic(type, nameof(ICurrency.MinorUnits))!;
            int numericCode = (int)ReadStatic(type, nameof(ICurrency.NumericCode))!;
            string englishName = (string)ReadStatic(type, nameof(ICurrency.EnglishName))!;

            Assert.AreEqual(type.Name, isoCode, $"{type.Name}: IsoCode must equal the tag type name.");
            Assert.IsTrue(numericCode is > 0 and <= 999, $"{isoCode}: numeric code {numericCode} is out of range.");
            Assert.IsTrue(minorUnits is >= 0 and <= 4, $"{isoCode}: minor units {minorUnits} is out of range.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(englishName), $"{isoCode}: English name must not be blank.");

            Assert.IsTrue(CurrencyRegistry.TryGet(isoCode, out CurrencyInfo? info), $"{isoCode}: missing from the registry.");
            Assert.AreEqual(numericCode, info!.NumericCode, $"{isoCode}: numeric-code mismatch with the registry.");
            Assert.AreEqual(minorUnits, info.MinorUnits, $"{isoCode}: minor-units mismatch with the registry.");
            Assert.AreEqual(englishName, info.EnglishName, $"{isoCode}: English-name mismatch with the registry.");
        }
    }

    /// <summary>
    /// Verifies that every shipped currency tag type honors a historic-currency contract: a historic tag must declare
    /// at least one of a demonetization date or a successor ISO code, and an active tag must declare neither.
    /// </summary>
    [TestMethod]
    public void CurrencyCatalogue_HistoricTagTypes_DeclareHistoricMetadata()
    {
        foreach (Type type in CurrencyTagTypes())
        {
            string isoCode = (string)ReadStatic(type, nameof(ICurrency.IsoCode))!;
            bool isHistoric = ReadStatic(type, nameof(ICurrency.IsHistoric)) as bool? ?? false;
            var demonetizedOn = ReadStatic(type, nameof(ICurrency.DemonetizedOn)) as DateOnly?;
            string? successorIsoCode = ReadStatic(type, nameof(ICurrency.SuccessorIsoCode)) as string;

            if (isHistoric)
                Assert.IsTrue(demonetizedOn is not null || successorIsoCode is not null, $"{isoCode}: a historic currency must record a demonetization date or a successor.");
            else
                Assert.IsTrue(demonetizedOn is null && successorIsoCode is null, $"{isoCode}: an active currency must not record demonetization metadata.");
        }
    }

    /// <summary>
    /// Verifies that every shipped currency tag type is sealed and exposes only a non-public parameterless
    /// constructor, so the tag types cannot be instantiated through ordinary code.
    /// </summary>
    [TestMethod]
    public void CurrencyCatalogue_EveryShippedTagType_IsSealedWithOnlyANonPublicConstructor()
    {
        foreach (Type type in CurrencyTagTypes())
        {
            Assert.IsEmpty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance), $"{type.Name}: must not expose a public constructor.");

            ConstructorInfo? constructor = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, binder: null, Type.EmptyTypes, modifiers: null);
            Assert.IsNotNull(constructor, $"{type.Name}: expected a non-public parameterless constructor.");

            // Invoke the generated constructor reflectively; the tag types are otherwise non-instantiable by design.
            Assert.IsNotNull(constructor!.Invoke(parameters: null));
        }
    }
}
