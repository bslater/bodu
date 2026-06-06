// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParserFieldMappingTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDateResourceLoader" /> maps every authored field onto the expected
/// <see cref="NotableDateDefinition" />, <see cref="NotableDateRule" />, <see cref="RuleApplicability" />, and strategy
/// values, and that each of the six strategy families parses to the correct concrete strategy with its fields. Ported
/// from the v1 <c>NotableDateRuleParserTests.ParseXml</c> / <c>NotableDateRuleJsonParserTests.ParseJson</c> field
/// assertions, mapped onto the v2 loaded-resource surface. The XML and JSON documents are kept structurally identical so
/// each assertion validates both ingestion paths.
/// </summary>
[TestClass]
public sealed class ParserFieldMappingTests
{
    /// <summary>
    /// A representative document expressed in XML, exercising the full field surface of one rich Fixed rule plus one
    /// rule per remaining strategy family.
    /// </summary>
    private const string Xml =
        """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.fields">
          <AdjustmentPolicies>
            <AdjustmentPolicy id="weekend-roll"><Trigger type="IfWeekend" /><Action type="MoveToNextWeekday" dayOfWeek="Monday" /><Emission mode="ObservedOnly" /></AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>

            <NotableDate id="new-years-day" displayName="New Year's Day" category="PublicHoliday" defaultNonWorkingDay="true" defaultDurationDays="2">
              <Tags><Tag value="public" /><Tag value="civic" /></Tags>
              <Rules>
                <Rule id="au-nsw" priority="5" category="Civic" nonWorking="false" durationDays="3">
                  <Applicability calendar="Gregorian" fromYear="2000" toYear="2100">
                    <Territory code="AU-NSW" />
                    <OnlyYear value="2024" />
                    <ExceptYear value="2025" />
                  </Applicability>
                  <Strategy><Fixed month="January" day="1" /></Strategy>
                  <Tags><Tag value="rule-tag" /></Tags>
                  <Adjustments><Adjustment policyRef="weekend-roll" /></Adjustments>
                </Rule>
              </Rules>
            </NotableDate>

            <NotableDate id="mothers-day" displayName="Mother's Day" category="Observance">
              <Rules>
                <Rule id="r"><Strategy><DayOfWeekInMonth month="May" dayOfWeek="Sunday" weekOrdinal="Second" /></Strategy></Rule>
              </Rules>
            </NotableDate>

            <NotableDate id="election-day" displayName="Election Day" category="Civic">
              <Rules>
                <Rule id="r"><Strategy><RelativeWeekdayInMonth month="November" dayOfWeek="Monday" weekOrdinal="First" relativeDayOfWeek="Tuesday" direction="After" /></Strategy></Rule>
              </Rules>
            </NotableDate>

            <NotableDate id="victoria-day" displayName="Victoria Day" category="PublicHoliday">
              <Rules>
                <Rule id="r"><Strategy><WeekdayNearDate month="May" day="24" dayOfWeek="Monday" direction="OnOrBefore" /></Strategy></Rule>
              </Rules>
            </NotableDate>

            <NotableDate id="easter-sunday" displayName="Easter Sunday" category="Religious">
              <Rules>
                <Rule id="western"><Strategy><Algorithm key="western-easter" /></Strategy></Rule>
              </Rules>
            </NotableDate>

            <NotableDate id="good-friday" displayName="Good Friday" category="Religious">
              <Rules>
                <Rule id="r"><Strategy><OffsetFromRule notableDateRef="easter-sunday" ruleRef="western" offsetDays="-2" /></Strategy></Rule>
              </Rules>
            </NotableDate>

          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// The same representative document expressed in JSON, with a one-to-one field correspondence to <see cref="Xml" />.
    /// </summary>
    private const string Json =
        """
        {
          "schemaVersion": "1.0", "resourceId": "test.fields",
          "adjustmentPolicies": [
            { "id": "weekend-roll", "trigger": { "type": "IfWeekend" }, "action": { "type": "MoveToNextWeekday", "dayOfWeek": "Monday" }, "emission": { "mode": "ObservedOnly" } }
          ],
          "notableDates": [
            { "id": "new-years-day", "displayName": "New Year's Day", "category": "PublicHoliday", "defaultNonWorkingDay": true, "defaultDurationDays": 2,
              "tags": [ "public", "civic" ],
              "rules": [
                { "id": "au-nsw", "priority": 5, "category": "Civic", "nonWorking": false, "durationDays": 3,
                  "applicability": { "calendar": "Gregorian", "fromYear": 2000, "toYear": 2100, "territories": [ "AU-NSW" ], "onlyYears": [ 2024 ], "exceptYears": [ 2025 ] },
                  "strategy": { "fixed": { "month": 1, "day": 1 } },
                  "tags": [ "rule-tag" ],
                  "adjustments": [ "weekend-roll" ] }
              ] },
            { "id": "mothers-day", "displayName": "Mother's Day", "category": "Observance",
              "rules": [ { "id": "r", "strategy": { "dayOfWeekInMonth": { "month": "May", "dayOfWeek": "Sunday", "weekOrdinal": "Second" } } } ] },
            { "id": "election-day", "displayName": "Election Day", "category": "Civic",
              "rules": [ { "id": "r", "strategy": { "relativeWeekdayInMonth": { "month": "November", "dayOfWeek": "Monday", "weekOrdinal": "First", "relativeDayOfWeek": "Tuesday", "direction": "After" } } } ] },
            { "id": "victoria-day", "displayName": "Victoria Day", "category": "PublicHoliday",
              "rules": [ { "id": "r", "strategy": { "weekdayNearDate": { "month": "May", "day": 24, "dayOfWeek": "Monday", "direction": "OnOrBefore" } } } ] },
            { "id": "easter-sunday", "displayName": "Easter Sunday", "category": "Religious",
              "rules": [ { "id": "western", "strategy": { "algorithm": { "key": "western-easter" } } } ] },
            { "id": "good-friday", "displayName": "Good Friday", "category": "Religious",
              "rules": [ { "id": "r", "strategy": { "offsetFromRule": { "notableDateRef": "easter-sunday", "ruleRef": "western", "offsetDays": -2 } } } ] }
          ]
        }
        """;

    /// <summary>
    /// The two ingestion paths to exercise: the XML loader and the JSON loader, returning the same field-rich resource.
    /// </summary>
    /// <returns>The named loader functions.</returns>
    public static IEnumerable<object[]> Loaders()
    {
        yield return new object[] { "xml", (Func<NotableDateResource>)(() => NotableDateResourceLoader.Load(Xml)) };
        yield return new object[] { "json", (Func<NotableDateResource>)(() => NotableDateResourceLoader.LoadJson(Json)) };
    }

    /// <summary>
    /// Finds the concept with the supplied identifier.
    /// </summary>
    /// <param name="resource">The loaded resource.</param>
    /// <param name="id">The concept identifier.</param>
    /// <returns>The matching concept.</returns>
    private static NotableDateDefinition Concept(NotableDateResource resource, string id) =>
        resource.NotableDates.Single(d => d.Id == id);

    /// <summary>
    /// Verifies that the concept-level metadata of the Fixed concept (id, display name, category, default non-working
    /// flag, default duration, and tags) maps from both ingestion paths.
    /// </summary>
    /// <param name="path">The ingestion-path label.</param>
    /// <param name="load">The loader to exercise.</param>
    [TestMethod]
    [DynamicData(nameof(Loaders), DynamicDataSourceType.Method)]
    public void Load_FixedConcept_ShouldMapConceptMetadata(string path, Func<NotableDateResource> load)
    {
        NotableDateDefinition concept = Concept(load(), "new-years-day");

        Assert.AreEqual("new-years-day", concept.Id, $"{path} id");
        Assert.AreEqual("New Year's Day", concept.DisplayName, $"{path} displayName");
        Assert.AreEqual(NotableDateCategory.PublicHoliday, concept.Category, $"{path} category");
        Assert.IsTrue(concept.DefaultNonWorkingDay, $"{path} defaultNonWorkingDay");
        Assert.AreEqual(2, concept.DefaultDurationDays, $"{path} defaultDurationDays");
        CollectionAssert.AreEqual(new[] { "public", "civic" }, concept.Tags.ToArray(), $"{path} tags");
    }

    /// <summary>
    /// Verifies that the rule-level overrides of the Fixed rule (id, priority, category override, non-working override,
    /// duration override, adjustment references, and rule tags) map from both ingestion paths.
    /// </summary>
    /// <param name="path">The ingestion-path label.</param>
    /// <param name="load">The loader to exercise.</param>
    [TestMethod]
    [DynamicData(nameof(Loaders), DynamicDataSourceType.Method)]
    public void Load_FixedRule_ShouldMapRuleFields(string path, Func<NotableDateResource> load)
    {
        NotableDateRule rule = Concept(load(), "new-years-day").Rules.Single();

        Assert.AreEqual("au-nsw", rule.Id, $"{path} id");
        Assert.AreEqual(5, rule.Priority, $"{path} priority");
        Assert.AreEqual(NotableDateCategory.Civic, rule.Category, $"{path} category override");
        Assert.AreEqual(false, rule.NonWorking, $"{path} nonWorking override");
        Assert.AreEqual(3, rule.DurationDays, $"{path} duration override");
        CollectionAssert.AreEqual(new[] { "weekend-roll" }, rule.AdjustmentPolicyRefs.ToArray(), $"{path} adjustments");
        CollectionAssert.AreEqual(new[] { "rule-tag" }, rule.Tags.ToArray(), $"{path} rule tags");
    }

    /// <summary>
    /// Verifies that the Fixed rule's applicability (calendar, year bounds, territories, only-years, and except-years)
    /// maps from both ingestion paths.
    /// </summary>
    /// <param name="path">The ingestion-path label.</param>
    /// <param name="load">The loader to exercise.</param>
    [TestMethod]
    [DynamicData(nameof(Loaders), DynamicDataSourceType.Method)]
    public void Load_FixedRule_ShouldMapApplicability(string path, Func<NotableDateResource> load)
    {
        RuleApplicability applicability = Concept(load(), "new-years-day").Rules.Single().Applicability;

        Assert.AreEqual(CalendarSystem.Gregorian, applicability.Calendar, $"{path} calendar");
        Assert.AreEqual(2000, applicability.FromYear, $"{path} fromYear");
        Assert.AreEqual(2100, applicability.ToYear, $"{path} toYear");
        CollectionAssert.AreEqual(new[] { "AU-NSW" }, applicability.Territories.ToArray(), $"{path} territories");
        CollectionAssert.AreEqual(new[] { 2024 }, applicability.OnlyYears.ToArray(), $"{path} onlyYears");
        CollectionAssert.AreEqual(new[] { 2025 }, applicability.ExceptYears.ToArray(), $"{path} exceptYears");
    }

    /// <summary>
    /// Verifies that the Fixed strategy maps to a <see cref="FixedDateStrategy" /> with the authored month, day, and
    /// Gregorian calendar from both ingestion paths.
    /// </summary>
    /// <param name="path">The ingestion-path label.</param>
    /// <param name="load">The loader to exercise.</param>
    [TestMethod]
    [DynamicData(nameof(Loaders), DynamicDataSourceType.Method)]
    public void Load_FixedStrategy_ShouldMapMonthAndDay(string path, Func<NotableDateResource> load)
    {
        var strategy = (FixedDateStrategy)Concept(load(), "new-years-day").Rules.Single().Strategy;

        Assert.AreEqual(1, strategy.Month, $"{path} month");
        Assert.AreEqual(1, strategy.Day, $"{path} day");
        Assert.AreEqual(CalendarSystem.Gregorian, strategy.Calendar, $"{path} calendar");
        Assert.IsNull(strategy.MonthAlias, $"{path} alias");
    }

    /// <summary>
    /// Verifies that the DayOfWeekInMonth strategy maps to a <see cref="DayOfWeekInMonthStrategy" /> with the authored
    /// month, weekday, and ordinal from both ingestion paths.
    /// </summary>
    /// <param name="path">The ingestion-path label.</param>
    /// <param name="load">The loader to exercise.</param>
    [TestMethod]
    [DynamicData(nameof(Loaders), DynamicDataSourceType.Method)]
    public void Load_DayOfWeekInMonthStrategy_ShouldMapFields(string path, Func<NotableDateResource> load)
    {
        var strategy = (DayOfWeekInMonthStrategy)Concept(load(), "mothers-day").Rules.Single().Strategy;

        Assert.AreEqual(5, strategy.Month, $"{path} month");
        Assert.AreEqual(DayOfWeek.Sunday, strategy.DayOfWeek, $"{path} dayOfWeek");
        Assert.AreEqual(WeekOrdinal.Second, strategy.WeekOrdinal, $"{path} weekOrdinal");
    }

    /// <summary>
    /// Verifies that the RelativeWeekdayInMonth strategy maps to a <see cref="RelativeWeekdayInMonthStrategy" /> with the
    /// authored anchor, ordinal, relative weekday, and direction from both ingestion paths.
    /// </summary>
    /// <param name="path">The ingestion-path label.</param>
    /// <param name="load">The loader to exercise.</param>
    [TestMethod]
    [DynamicData(nameof(Loaders), DynamicDataSourceType.Method)]
    public void Load_RelativeWeekdayInMonthStrategy_ShouldMapFields(string path, Func<NotableDateResource> load)
    {
        var strategy = (RelativeWeekdayInMonthStrategy)Concept(load(), "election-day").Rules.Single().Strategy;

        Assert.AreEqual(11, strategy.Month, $"{path} month");
        Assert.AreEqual(DayOfWeek.Monday, strategy.DayOfWeek, $"{path} anchor dayOfWeek");
        Assert.AreEqual(WeekOrdinal.First, strategy.WeekOrdinal, $"{path} weekOrdinal");
        Assert.AreEqual(DayOfWeek.Tuesday, strategy.RelativeDayOfWeek, $"{path} relativeDayOfWeek");
        Assert.AreEqual(WeekdayProximity.After, strategy.Direction, $"{path} direction");
    }

    /// <summary>
    /// Verifies that the WeekdayNearDate strategy maps to a <see cref="WeekdayNearDateStrategy" /> with the authored
    /// month, day, weekday, and direction from both ingestion paths.
    /// </summary>
    /// <param name="path">The ingestion-path label.</param>
    /// <param name="load">The loader to exercise.</param>
    [TestMethod]
    [DynamicData(nameof(Loaders), DynamicDataSourceType.Method)]
    public void Load_WeekdayNearDateStrategy_ShouldMapFields(string path, Func<NotableDateResource> load)
    {
        var strategy = (WeekdayNearDateStrategy)Concept(load(), "victoria-day").Rules.Single().Strategy;

        Assert.AreEqual(5, strategy.Month, $"{path} month");
        Assert.AreEqual(24, strategy.Day, $"{path} day");
        Assert.AreEqual(DayOfWeek.Monday, strategy.DayOfWeek, $"{path} dayOfWeek");
        Assert.AreEqual(WeekdayProximity.OnOrBefore, strategy.Direction, $"{path} direction");
    }

    /// <summary>
    /// Verifies that the Algorithm strategy maps to an <see cref="AlgorithmDateStrategy" /> with the authored key from
    /// both ingestion paths.
    /// </summary>
    /// <param name="path">The ingestion-path label.</param>
    /// <param name="load">The loader to exercise.</param>
    [TestMethod]
    [DynamicData(nameof(Loaders), DynamicDataSourceType.Method)]
    public void Load_AlgorithmStrategy_ShouldMapKey(string path, Func<NotableDateResource> load)
    {
        var strategy = (AlgorithmDateStrategy)Concept(load(), "easter-sunday").Rules.Single().Strategy;

        Assert.AreEqual("western-easter", strategy.Key, $"{path} key");
    }

    /// <summary>
    /// Verifies that the OffsetFromRule strategy maps to an <see cref="OffsetFromRuleStrategy" /> with the authored
    /// notable-date reference, rule reference, and signed day offset from both ingestion paths.
    /// </summary>
    /// <param name="path">The ingestion-path label.</param>
    /// <param name="load">The loader to exercise.</param>
    [TestMethod]
    [DynamicData(nameof(Loaders), DynamicDataSourceType.Method)]
    public void Load_OffsetFromRuleStrategy_ShouldMapReferenceAndOffset(string path, Func<NotableDateResource> load)
    {
        var strategy = (OffsetFromRuleStrategy)Concept(load(), "good-friday").Rules.Single().Strategy;

        Assert.AreEqual("easter-sunday", strategy.NotableDateRef, $"{path} notableDateRef");
        Assert.AreEqual("western", strategy.RuleRef, $"{path} ruleRef");
        Assert.AreEqual(-2, strategy.OffsetDays, $"{path} offsetDays");
    }

    /// <summary>
    /// Verifies that the resource exposes the authored resource-level metadata and concept and rule counts from both
    /// ingestion paths.
    /// </summary>
    /// <param name="path">The ingestion-path label.</param>
    /// <param name="load">The loader to exercise.</param>
    [TestMethod]
    [DynamicData(nameof(Loaders), DynamicDataSourceType.Method)]
    public void Load_Document_ShouldMapResourceMetadataAndCounts(string path, Func<NotableDateResource> load)
    {
        NotableDateResource resource = load();

        Assert.AreEqual("test.fields", resource.ResourceId, $"{path} resourceId");
        Assert.AreEqual("1.0", resource.SchemaVersion, $"{path} schemaVersion");
        Assert.AreEqual(6, resource.NotableDates.Count, $"{path} concept count");
        Assert.AreEqual(6, resource.RuleCount, $"{path} rule count");
        Assert.AreEqual(1, resource.AdjustmentPolicies.Count, $"{path} policy count");
    }

    /// <summary>
    /// Verifies that an adjustment policy maps its id, scope-bound trigger, action, action weekday, and emission mode
    /// from both ingestion paths.
    /// </summary>
    /// <param name="path">The ingestion-path label.</param>
    /// <param name="load">The loader to exercise.</param>
    [TestMethod]
    [DynamicData(nameof(Loaders), DynamicDataSourceType.Method)]
    public void Load_AdjustmentPolicy_ShouldMapTriggerActionEmission(string path, Func<NotableDateResource> load)
    {
        AdjustmentPolicy policy = load().AdjustmentPolicies.Single();

        Assert.AreEqual("weekend-roll", policy.Id, $"{path} id");
        Assert.AreEqual(AdjustmentTrigger.IfWeekend, policy.Trigger, $"{path} trigger");
        Assert.AreEqual(AdjustmentAction.MoveToNextWeekday, policy.Action, $"{path} action");
        Assert.AreEqual(DayOfWeek.Monday, policy.ActionWeekday, $"{path} actionWeekday");
        Assert.AreEqual(EmissionMode.ObservedOnly, policy.Emission, $"{path} emission");
    }

    /// <summary>
    /// Verifies that a rule omitting its optional category, non-working, and duration overrides leaves those values
    /// unset so the parent concept's defaults apply, from both ingestion paths.
    /// </summary>
    /// <param name="path">The ingestion-path label.</param>
    /// <param name="load">The loader to exercise.</param>
    [TestMethod]
    [DynamicData(nameof(Loaders), DynamicDataSourceType.Method)]
    public void Load_RuleWithoutOverrides_ShouldLeaveOverridesNull(string path, Func<NotableDateResource> load)
    {
        NotableDateRule rule = Concept(load(), "mothers-day").Rules.Single();

        Assert.IsNull(rule.Category, $"{path} category");
        Assert.IsNull(rule.NonWorking, $"{path} nonWorking");
        Assert.IsNull(rule.DurationDays, $"{path} durationDays");
        Assert.AreEqual(0, rule.AdjustmentPolicyRefs.Count, $"{path} adjustments");
    }
}
