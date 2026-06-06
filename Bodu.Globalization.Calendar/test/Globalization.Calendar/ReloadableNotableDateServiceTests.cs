// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReloadableNotableDateServiceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="ReloadableNotableDateService" /> observes a resource swapped through a
/// <see cref="MutableNotableDateResourceProvider" />, including override changes applied by reloading.
/// </summary>
[TestClass]
public sealed class ReloadableNotableDateServiceTests
{
    /// <summary>
    /// A resource placing the special day on 1 January.
    /// </summary>
    private const string JanuaryXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.reload">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="special-day" displayName="Special Day" category="PublicHoliday" defaultNonWorkingDay="false">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// A resource placing the special day on 1 February.
    /// </summary>
    private const string FebruaryXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.reload">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="special-day" displayName="Special Day" category="PublicHoliday" defaultNonWorkingDay="false">
          <Rules><Rule id="x"><Strategy><Fixed month="February" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// A two-concept resource.
    /// </summary>
    private const string TwoConceptXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.two">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="keep" displayName="Keep" category="PublicHoliday">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="drop" displayName="Drop" category="PublicHoliday">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="2" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// The two-concept resource with an override that removes the second concept's only rule.
    /// </summary>
    private const string TwoConceptDroppedXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.two">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="keep" displayName="Keep" category="PublicHoliday">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="drop" displayName="Drop" category="PublicHoliday">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="2" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
      <Overrides>
        <RemoveRule notableDateRef="drop" ruleRef="x" />
      </Overrides>
    </NotableDateResource>
    """;

    /// <summary>
    /// The inclusive 2025 calendar year range.
    /// </summary>
    private static readonly DateRange Year2025 = new(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));

    /// <summary>
    /// Verifies that the service resolves against the provider's initial resource before any reload.
    /// </summary>
    [TestMethod]
    public void Resolve_BeforeReload_ShouldUseInitialResource()
    {
        MutableNotableDateResourceProvider provider = new(NotableDateResourceLoader.Load(JanuaryXml));
        ReloadableNotableDateService service = new(provider);

        NotableDate match = service.Resolve(Year2025, "XX").Single(r => r.NotableDateId == "special-day");

        Assert.AreEqual(new DateOnly(2025, 1, 1), match.Date);
    }

    /// <summary>
    /// Verifies that reloading the provider's resource changes what the service resolves on the next query.
    /// </summary>
    [TestMethod]
    public void Resolve_AfterReload_ShouldReflectNewResource()
    {
        MutableNotableDateResourceProvider provider = new(NotableDateResourceLoader.Load(JanuaryXml));
        ReloadableNotableDateService service = new(provider);

        _ = service.Resolve(Year2025, "XX");

        provider.Reload(NotableDateResourceLoader.Load(FebruaryXml));

        NotableDate match = service.Resolve(Year2025, "XX").Single(r => r.NotableDateId == "special-day");

        Assert.AreEqual(new DateOnly(2025, 2, 1), match.Date);
    }

    /// <summary>
    /// Verifies that override operations applied by reloading a rebuilt resource take effect at runtime.
    /// </summary>
    [TestMethod]
    public void Resolve_AfterReloadWithOverride_ShouldApplyRuntimeOverride()
    {
        MutableNotableDateResourceProvider provider = new(NotableDateResourceLoader.Load(TwoConceptXml));
        ReloadableNotableDateService service = new(provider);

        Assert.HasCount(2, service.Resolve(Year2025, "XX"));

        provider.Reload(NotableDateResourceLoader.Load(TwoConceptDroppedXml));

        IReadOnlyList<NotableDate> after = service.Resolve(Year2025, "XX");
        Assert.HasCount(1, after);
        Assert.AreEqual("keep", after[0].NotableDateId);
    }

    /// <summary>
    /// Verifies that reloading the provider with a <see langword="null" /> resource throws.
    /// </summary>
    [TestMethod]
    public void Reload_WhenResourceIsNull_ShouldThrowArgumentNullException()
    {
        MutableNotableDateResourceProvider provider = new(NotableDateResourceLoader.Load(JanuaryXml));

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            provider.Reload(null!);
        });
    }
}
