// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CommonResourcesTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the bundled common notable-date catalogues and their resolver: that the embedded catalogues are
/// discoverable and that a territory resource can import the shared concepts and specialize them with its own
/// territory scope, category, non-working flag, and weekend adjustments.
/// </summary>
[TestClass]
public sealed class CommonResourcesTests
{
    /// <summary>
    /// Verifies that the resolver returns content for a bundled catalogue and <see langword="null" /> for an unknown
    /// name.
    /// </summary>
    /// <param name="name">The catalogue name to resolve.</param>
    /// <param name="expectContent">Whether the resolver is expected to return content.</param>
    [TestMethod]
    [DataRow("global-core", true)]         // bundled catalogue
    [DataRow("christian-western", true)]   // bundled catalogue
    [DataRow("no-such-catalogue", false)]  // unknown name
    public void Resolve_ForBundledCatalogue_ReturnsContentForKnownAndNullForUnknown(string name, bool expectContent)
    {
        Assert.AreEqual(expectContent, CommonNotableDateResources.Resolve(name) is not null);
    }

    /// <summary>
    /// Verifies that a strongly-typed catalogue resolves to the same content as its raw resource name, so the enum
    /// overload is a faithful alias for the string overload.
    /// </summary>
    [TestMethod]
    public void Resolve_ForStronglyTypedCatalog_ReturnsSameContentAsName()
    {
        Assert.AreEqual(
            CommonNotableDateResources.Resolve("global-core"),
            CommonNotableDateResources.Resolve(CommonNotableDateCatalog.GlobalCore));
    }

    /// <summary>
    /// Verifies that <see cref="CommonNotableDateResources.Resolve(CommonNotableDateCatalog)" /> throws for a value that
    /// is not a defined <see cref="CommonNotableDateCatalog" />.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenCatalogUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = CommonNotableDateResources.Resolve((CommonNotableDateCatalog)(-1));
        });

        Assert.AreEqual("catalog", ex.ParamName);
    }

    /// <summary>
    /// Verifies that loading the bundled <c>global-all</c> catalogue by its strongly-typed name materializes a populated
    /// <see cref="NotableDateResource" /> whose nested imports resolve against the other bundled catalogues.
    /// </summary>
    [TestMethod]
    public void Load_ForGlobalAllCatalog_ReturnsPopulatedResource()
    {
        NotableDateResource resource = CommonNotableDateResources.Load(CommonNotableDateCatalog.GlobalAll);

        Assert.IsNotEmpty(resource.NotableDates);
    }

    /// <summary>
    /// Verifies that <see cref="CommonNotableDateResources.Load(CommonNotableDateCatalog)" /> throws for a value that is
    /// not a defined <see cref="CommonNotableDateCatalog" />.
    /// </summary>
    [TestMethod]
    public void Load_WhenCatalogUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = CommonNotableDateResources.Load((CommonNotableDateCatalog)(-1));
        });

        Assert.AreEqual("catalog", ex.ParamName);
    }

    /// <summary>
    /// Verifies that every <see cref="CommonNotableDateCatalog" /> member maps to a bundled catalogue that resolves to
    /// content, so the enum and the embedded resources stay in lock-step.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void GetResourceName_ForEveryCatalog_MapsToBundledResource()
    {
        foreach (CommonNotableDateCatalog catalog in Enum.GetValues<CommonNotableDateCatalog>())
        {
            string name = CommonNotableDateResources.GetResourceName(catalog);
            Assert.IsNotNull(
                CommonNotableDateResources.Resolve(name),
                $"catalogue '{catalog}' maps to '{name}', which did not resolve to bundled content");
        }
    }

    /// <summary>
    /// A territory resource importing the bundled common catalogues and specializing several shared concepts with the
    /// territory's own scope, category, non-working flag, and weekend adjustments.
    /// </summary>
    private const string Region = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.demo">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <AdjustmentPolicies>
        <AdjustmentPolicy id="sat-to-fri" priority="100">
          <Trigger type="IfDayOfWeek"><Weekday value="Saturday" /></Trigger>
          <Action type="MoveToPreviousWeekday" dayOfWeek="Friday" />
          <Emission mode="ObservedOnly" reason="Observed Friday" />
        </AdjustmentPolicy>
        <AdjustmentPolicy id="sun-to-mon" priority="100">
          <Trigger type="IfDayOfWeek"><Weekday value="Sunday" /></Trigger>
          <Action type="MoveToNextWeekday" dayOfWeek="Monday" />
          <Emission mode="ObservedOnly" reason="Observed Monday" />
        </AdjustmentPolicy>
      </AdjustmentPolicies>
      <Imports>
        <Import resource="global-core">
          <Use notableDateRef="new-years-day" territory="ZZ">
            <Adjustments><Adjustment policyRef="sat-to-fri" /><Adjustment policyRef="sun-to-mon" /></Adjustments>
          </Use>
        </Import>
        <Import resource="christian-western">
          <Use notableDateRef="easter-sunday" territory="ZZ" />
          <Use notableDateRef="good-friday" territory="ZZ" category="Religious" nonWorking="false" />
          <Use notableDateRef="christmas-day" territory="ZZ">
            <Adjustments><Adjustment policyRef="sat-to-fri" /><Adjustment policyRef="sun-to-mon" /></Adjustments>
          </Use>
        </Import>
      </Imports>
      <NotableDates />
    </NotableDateResource>
    """;

    /// <summary>
    /// Builds a service over the territory <see cref="Region" /> resource resolved against the bundled common catalogues.
    /// </summary>
    /// <returns>A service for the region resource.</returns>
    private static NotableDateService RegionService() =>
        new(NotableDateResourceLoader.Load(Region, CommonNotableDateResources.Resolver));

    /// <summary>
    /// Verifies that a territory resource importing the bundled catalogues computes the Easter-anchored offset. Easter
    /// Sunday 2024 resolves to 31 March.
    /// </summary>
    [TestMethod]
    public void Import_FromBundledCatalogues_ResolvesEasterAnchoredOffset()
    {
        DateRange year2024 = new(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        Assert.AreEqual(new DateOnly(2024, 3, 31), RegionService().Resolve(year2024, "ZZ").Single(r => r.NotableDateId == "easter-sunday").Date);
    }

    /// <summary>
    /// Verifies that a territory resource importing the bundled catalogues applies its category and non-working overrides.
    /// The imported Good Friday (29 March 2024) carries the territory's Religious category and working-day override.
    /// </summary>
    [TestMethod]
    public void Import_FromBundledCatalogues_AppliesGoodFridayOverrides()
    {
        NotableDate goodFriday = RegionService().Resolve(new DateOnly(2024, 3, 29), "ZZ").Single(r => r.NotableDateId == "good-friday");

        Assert.AreEqual(
            (NotableDateCategory.Religious, false),
            (goodFriday.Category, goodFriday.IsNonWorkingDay),
            "category and non-working overrides applied");
    }

    /// <summary>
    /// Verifies that a territory resource importing the bundled catalogues applies its own weekend adjustment. New Year's
    /// Day 2023 (a Sunday) is observed on Monday 2 January, carrying the actual date.
    /// </summary>
    [TestMethod]
    public void Import_FromBundledCatalogues_ObservesNewYearOverride()
    {
        NotableDate newYear = RegionService().Resolve(new DateOnly(2023, 1, 2), "ZZ").Single(r => r.NotableDateId == "new-years-day");

        Assert.AreEqual(
            (true, (DateOnly?)new DateOnly(2023, 1, 1)),
            (newYear.IsObserved, newYear.ActualDate));
    }

    /// <summary>
    /// Verifies that every bundled common catalogue parses and passes semantic validation when loaded through the
    /// shared resolver, so an authoring error in any catalogue fails the build.
    /// </summary>
    [TestMethod]
    public void AllBundledCatalogues_LoadAndValidate()
    {
        System.Reflection.Assembly assembly = typeof(CommonNotableDateResources).Assembly;
        const string prefix = "Bodu.Globalization.Calendar.Resources.";
        var catalogues = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(prefix, StringComparison.Ordinal) && name.EndsWith(".xml", StringComparison.Ordinal))
            .Select(name => name[prefix.Length..^4])
            .ToList();

        Assert.IsGreaterThanOrEqualTo(2, catalogues.Count, "expected the bundled catalogues to be embedded");

        foreach (string? catalogue in catalogues)
        {
            string? content = CommonNotableDateResources.Resolve(catalogue);
            Assert.IsNotNull(content, $"catalogue '{catalogue}' did not resolve");

            NotableDateResource resource = NotableDateResourceLoader.Load(content!, CommonNotableDateResources.Resolver);
            Assert.IsNotEmpty(resource.NotableDates, $"catalogue '{catalogue}' has no concepts");
        }
    }
}
