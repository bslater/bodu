// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilderTests.JsonSubset.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using Bodu.Globalization.Calendar.RangeResolution;

namespace Bodu.Globalization.Calendar.Builder;

public partial class NotableDateDocumentBuilderTests
{
    /// <summary>
    /// Builds a minimal, JSON-serializable document carrying a single fixed-date concept, used as the base onto which a
    /// single unsupported feature is grafted in each rejection case.
    /// </summary>
    /// <returns>A minimal document builder.</returns>
    private static NotableDateDocumentBuilder JsonRejectionBase() =>
        NotableDateDocumentBuilder.Create("demo.reject")
            .AddNotableDate("nd", "ND", NotableDateCategory.Observance, d => d
                .AddRule("r", x => x.Fixed(1, 1)));

    /// <summary>
    /// Provides one row per feature that the JSON subset cannot represent, each grafting a single offending feature onto
    /// the minimal base so that <see cref="NotableDateDocumentBuilder.ToJson" /> rejects it.
    /// </summary>
    /// <returns>One <c>(scenario, factory)</c> pair per unsupported feature, for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> UnsupportedJsonFeatureCases()
    {
        static object[] Case(string scenario, Func<NotableDateDocumentBuilder> build) =>
            [scenario, build];

        yield return Case("unsupported trigger", () => JsonRejectionBase().AddAdjustmentPolicy("p", a => a
            .When(AdjustmentTrigger.IfBeforeFixedDate).Then(AdjustmentAction.AddDays).WithActionDays(1).Emit(EmissionMode.ObservedOnly)));
        yield return Case("unsupported action", () => JsonRejectionBase().AddAdjustmentPolicy("p", a => a
            .When(AdjustmentTrigger.IfWeekend).Then(AdjustmentAction.ReplaceWithRule).Emit(EmissionMode.ObservedOnly)));
        yield return Case("trigger month", () => JsonRejectionBase().AddAdjustmentPolicy("p", a => a
            .When(AdjustmentTrigger.IfWeekend).WithTriggerMonth(6).Then(AdjustmentAction.AddDays).WithActionDays(1).Emit(EmissionMode.ObservedOnly)));
        yield return Case("trigger day", () => JsonRejectionBase().AddAdjustmentPolicy("p", a => a
            .When(AdjustmentTrigger.IfWeekend).WithTriggerDay(15).Then(AdjustmentAction.AddDays).WithActionDays(1).Emit(EmissionMode.ObservedOnly)));
        yield return Case("trigger weekOrdinal", () => JsonRejectionBase().AddAdjustmentPolicy("p", a => a
            .When(AdjustmentTrigger.IfWeekend).WithTriggerWeekOrdinal(WeekOrdinal.Second).Then(AdjustmentAction.AddDays).WithActionDays(1).Emit(EmissionMode.ObservedOnly)));
        yield return Case("trigger handlerKey", () => JsonRejectionBase().AddAdjustmentPolicy("p", a => a
            .When(AdjustmentTrigger.IfWeekend).WithTriggerHandlerKey("trig").Then(AdjustmentAction.AddDays).WithActionDays(1).Emit(EmissionMode.ObservedOnly)));
        yield return Case("action notableDateRef", () => JsonRejectionBase().AddAdjustmentPolicy("p", a => a
            .When(AdjustmentTrigger.IfWeekend).Then(AdjustmentAction.MoveToNextWorkingDay).WithReplacementRule("other").Emit(EmissionMode.ObservedOnly)));
        yield return Case("action handlerKey", () => JsonRejectionBase().AddAdjustmentPolicy("p", a => a
            .When(AdjustmentTrigger.IfWeekend).Then(AdjustmentAction.MoveToNextWeekday).WithActionHandlerKey("h").Emit(EmissionMode.ObservedOnly)));
        yield return Case("handler parameters", () => JsonRejectionBase().AddAdjustmentPolicy("p", a => a
            .When(AdjustmentTrigger.IfWeekend).Then(AdjustmentAction.AddDays).WithActionDays(1).WithParameter("k", "v").Emit(EmissionMode.ObservedOnly)));

        yield return Case("scope year bounds", () => JsonRejectionBase().AddAdjustmentPolicy("p", a => a
            .When(AdjustmentTrigger.IfWeekend).Then(AdjustmentAction.MoveToNextWorkingDay).Emit(EmissionMode.ObservedOnly).WithScope(s => s.FromYear(2000))));
        yield return Case("scope OnlyYear", () => JsonRejectionBase().AddAdjustmentPolicy("p", a => a
            .When(AdjustmentTrigger.IfWeekend).Then(AdjustmentAction.MoveToNextWorkingDay).Emit(EmissionMode.ObservedOnly).WithScope(s => s.OnlyYear(2025))));
        yield return Case("scope ExceptYear", () => JsonRejectionBase().AddAdjustmentPolicy("p", a => a
            .When(AdjustmentTrigger.IfWeekend).Then(AdjustmentAction.MoveToNextWorkingDay).Emit(EmissionMode.ObservedOnly).WithScope(s => s.ExceptYear(2026))));
        yield return Case("scope NotableDate reference", () => JsonRejectionBase().AddAdjustmentPolicy("p", a => a
            .When(AdjustmentTrigger.IfWeekend).Then(AdjustmentAction.MoveToNextWorkingDay).Emit(EmissionMode.ObservedOnly).WithScope(s => s.ForNotableDate("x"))));
        yield return Case("scope Rule reference", () => JsonRejectionBase().AddAdjustmentPolicy("p", a => a
            .When(AdjustmentTrigger.IfWeekend).Then(AdjustmentAction.MoveToNextWorkingDay).Emit(EmissionMode.ObservedOnly).WithScope(s => s.ForRule("nd", "r"))));

        yield return Case("override patch applicability", () => JsonRejectionBase().AddOverride(o => o
            .PatchRule("nd", "r", x => x.ForTerritory("US"))));
        yield return Case("override patch strategy", () => JsonRejectionBase().AddOverride(o => o
            .PatchRule("nd", "r", x => x.Fixed(1, 1))));
        yield return Case("override patch tags", () => JsonRejectionBase().AddOverride(o => o
            .PatchRule("nd", "r", x => x.AddTag("t"))));
        yield return Case("override patch adjustments", () => JsonRejectionBase().AddOverride(o => o
            .PatchRule("nd", "r", x => x.WithAdjustment("a"))));
    }

    /// <summary>
    /// Renders a readable display name for an <see cref="UnsupportedJsonFeatureCases" /> row using its scenario label.
    /// </summary>
    /// <param name="methodInfo">The test method being parameterized.</param>
    /// <param name="data">The row's data values.</param>
    /// <returns>The composed display name.</returns>
    public static string FormatJsonRejectionCase(MethodInfo methodInfo, object?[] data) =>
        string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}({1})", methodInfo.Name, data[0]);

    /// <summary>
    /// Verifies that serializing a document whose single offending feature lies outside the JSON subset throws
    /// <see cref="NotSupportedException" /> naming the feature.
    /// </summary>
    /// <param name="scenario">The human-readable scenario label.</param>
    /// <param name="build">A factory producing the offending document.</param>
    [TestMethod]
    [DynamicData(nameof(UnsupportedJsonFeatureCases), DynamicDataDisplayName = nameof(FormatJsonRejectionCase))]
    public void ToJson_WhenDocumentUsesUnsupportedJsonFeature_ShouldThrowNotSupportedException(string scenario, Func<NotableDateDocumentBuilder> build)
    {
        _ = scenario;
        NotableDateDocumentBuilder builder = build();

        _ = Assert.ThrowsExactly<NotSupportedException>(builder.ToJson);
    }

    /// <summary>
    /// Verifies that an action carrying a replacement <c>ruleRef</c> without a <c>notableDateRef</c> — reachable only by
    /// parsing XML — is rejected when serialized to JSON, because the JSON subset cannot represent it.
    /// </summary>
    [TestMethod]
    public void ToJson_WhenActionCarriesRuleRefWithoutNotableDateRef_ShouldThrowNotSupportedException()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="demo.ruleref">
              <AdjustmentPolicies>
                <AdjustmentPolicy id="p">
                  <Trigger type="IfWeekend" />
                  <Action type="MoveToNextWorkingDay" ruleRef="some-rule" />
                  <Emission mode="ObservedOnly" />
                </AdjustmentPolicy>
              </AdjustmentPolicies>
              <NotableDates>
                <NotableDate id="nd" displayName="ND" category="Observance">
                  <Rules><Rule id="r"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        var builder = NotableDateDocumentBuilder.FromXml(xml);

        _ = Assert.ThrowsExactly<NotSupportedException>(builder.ToJson);
    }

    /// <summary>
    /// Verifies that an adjustment scope confined to the JSON-representable axes (territories, calendars, and categories)
    /// round-trips through JSON unchanged.
    /// </summary>
    [TestMethod]
    public void ToJsonFromJson_WhenScopeIsJsonRepresentable_ShouldRoundTrip()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create("demo.scope")
            .AddAdjustmentPolicy("p", a => a
                .When(AdjustmentTrigger.IfWeekend)
                .Then(AdjustmentAction.MoveToNextWorkingDay)
                .Emit(EmissionMode.ObservedOnly)
                .WithScope(s => s
                    .ForTerritory("US")
                    .ForCalendar(CalendarSystem.Gregorian)
                    .ForCategory(NotableDateCategory.PublicHoliday)))
            .AddNotableDate("nd", "ND", NotableDateCategory.Observance, d => d
                .AddRule("r", x => x.Fixed(1, 1)));

        string json = builder.ToJson();

        Assert.AreEqual(json, NotableDateDocumentBuilder.FromJson(json).ToJson());
    }

    /// <summary>
    /// Verifies that a fixed-date strategy carrying the lunisolar <c>skipLeapMonth</c> and <c>sweepCalendarYears</c>
    /// flags round-trips through JSON unchanged, exercising the Boolean strategy-attribute mapping in both directions.
    /// </summary>
    [TestMethod]
    public void ToJsonFromJson_WhenFixedStrategyCarriesLunisolarFlags_ShouldRoundTrip()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create("demo.leap")
            .AddNotableDate("nd", "ND", NotableDateCategory.Observance, d => d
                .AddRule("r", x => x.Fixed(2, 29, skipLeapMonth: true, sweepCalendarYears: true)));

        string json = builder.ToJson();

        Assert.AreEqual(json, NotableDateDocumentBuilder.FromJson(json).ToJson());
    }

    /// <summary>
    /// Verifies that serializing a concept that declares no rules throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void ToJson_WhenConceptHasNoRules_ShouldThrowInvalidOperationException()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create("demo.norules")
            .AddNotableDate("nd", "ND", NotableDateCategory.Observance, d => { });

        _ = Assert.ThrowsExactly<InvalidOperationException>(builder.ToJson);
    }

    /// <summary>
    /// Verifies that serializing a rule that declares no strategy throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void ToJson_WhenRuleHasNoStrategy_ShouldThrowInvalidOperationException()
    {
        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create("demo.nostrategy")
            .AddNotableDate("nd", "ND", NotableDateCategory.Observance, d => d
                .AddRule("r", x => { }));

        _ = Assert.ThrowsExactly<InvalidOperationException>(builder.ToJson);
    }

    /// <summary>
    /// Verifies that parsing malformed JSON throws <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void FromJson_WhenJsonMalformed_ShouldThrowFormatException()
    {
        _ = Assert.ThrowsExactly<FormatException>(() => NotableDateDocumentBuilder.FromJson("{ not json"));
    }

    /// <summary>
    /// Verifies that parsing JSON whose root is not an object throws <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void FromJson_WhenRootIsNotJsonObject_ShouldThrowFormatException()
    {
        _ = Assert.ThrowsExactly<FormatException>(() => NotableDateDocumentBuilder.FromJson("[]"));
    }

    /// <summary>
    /// Verifies that parsing a document that omits the optional metadata, resolution-policy, adjustment-policy,
    /// notable-date, and override sections succeeds, retaining the resource identifier.
    /// </summary>
    [TestMethod]
    public void FromJson_WhenOptionalSectionsAbsent_ShouldRetainResourceId()
    {
        var builder = NotableDateDocumentBuilder.FromJson("""{ "resourceId": "demo.min" }""");

        Assert.Contains("demo.min", builder.ToJson());
    }

    /// <summary>
    /// Wraps a rule JSON fragment in a minimal document carrying a single concept, for parse-edge testing.
    /// </summary>
    /// <param name="ruleJson">The rule object JSON.</param>
    /// <returns>The wrapped document JSON.</returns>
    private static string JsonWithRule(string ruleJson) =>
        $$"""
        {
          "resourceId": "demo.rule",
          "notableDates": [
            { "id": "nd", "displayName": "ND", "category": "Observance", "rules": [ {{ruleJson}} ] }
          ]
        }
        """;

    /// <summary>
    /// Verifies that a JSON strategy attribute is normalized into the XML form: a numeric month becomes its English
    /// month name, a Boolean flag becomes its lowercase literal, and a JSON <c>null</c> becomes an empty attribute.
    /// </summary>
    /// <param name="strategyJson">The strategy object JSON.</param>
    /// <param name="expectedFragment">The XML fragment expected after normalization.</param>
    [TestMethod]
    [DataRow("""{ "fixed": { "month": 1, "day": 1 } }""", "month=\"January\"", DisplayName = "numeric month")]
    [DataRow("""{ "fixed": { "month": "January", "day": 1, "skipLeapMonth": false } }""", "skipLeapMonth=\"false\"", DisplayName = "boolean false")]
    [DataRow("""{ "fixed": { "month": "January", "day": 1, "skipLeapMonth": null } }""", "skipLeapMonth=\"\"", DisplayName = "null attribute")]
    public void FromJson_WhenStrategyAttributeVaries_ShouldNormalizeInXml(string strategyJson, string expectedFragment)
    {
        string xml = NotableDateDocumentBuilder.FromJson(JsonWithRule($$"""{ "id": "r", "strategy": {{strategyJson}} }""")).ToXml();

        Assert.Contains(expectedFragment, xml);
    }

    /// <summary>
    /// Verifies that a rule whose strategy property is absent, empty, or unrecognized parses into a strategy-less rule —
    /// observable because such a rule cannot be re-serialized.
    /// </summary>
    /// <param name="ruleJson">The rule object JSON.</param>
    [TestMethod]
    [DataRow("""{ "id": "r" }""", DisplayName = "no strategy property")]
    [DataRow("""{ "id": "r", "strategy": { } }""", DisplayName = "empty strategy")]
    [DataRow("""{ "id": "r", "strategy": { "unknownKey": 5 } }""", DisplayName = "unrecognized strategy")]
    public void FromJson_WhenRuleHasNoUsableStrategy_ShouldProduceStrategylessRule(string ruleJson)
    {
        var builder = NotableDateDocumentBuilder.FromJson(JsonWithRule(ruleJson));

        _ = Assert.ThrowsExactly<InvalidOperationException>(builder.ToXml);
    }
}
