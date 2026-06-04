// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParserEnumSurfaceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Verifies that <see cref="NotableDateResourceLoader" /> resolves every supported value of each enumeration that
/// participates in the XML and JSON authoring vocabulary — <see cref="NotableDateCategory" />,
/// <see cref="AdjustmentTrigger" />, <see cref="AdjustmentAction" />, <see cref="EmissionMode" />,
/// <see cref="System.DayOfWeek" />, <see cref="WeekOrdinal" />, and <see cref="WeekdayProximity" />. Ported from the v1
/// <c>NotableDateRuleParserTests.EnumSurface</c> / <c>NotableDateRuleJsonParserTests.EnumSurface</c> rows, mapped onto
/// the v2 loaded-resource surface (the v1 <c>Holiday</c>/<c>Bank</c> categories are renamed to
/// <see cref="NotableDateCategory.PublicHoliday" />/<see cref="NotableDateCategory.BankHoliday" />).
/// </summary>
[TestClass]
public sealed class ParserEnumSurfaceTests
{
    /// <summary>
    /// Builds a one-concept XML document whose notable-date carries the supplied category token.
    /// </summary>
    /// <param name="category">The category token to author.</param>
    /// <returns>The XML document content.</returns>
    private static string CategoryXml(string category) =>
        $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.enum">
          <NotableDates>
            <NotableDate id="x" displayName="X" category="{category}">
              <Rules><Rule id="r"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// Builds a one-concept JSON document whose notable-date carries the supplied category token.
    /// </summary>
    /// <param name="category">The category token to author.</param>
    /// <returns>The JSON document content.</returns>
    private static string CategoryJson(string category) =>
        $$"""
        {
          "schemaVersion": "1.0", "resourceId": "test.enum",
          "notableDates": [
            { "id": "x", "displayName": "X", "category": "{{category}}",
              "rules": [ { "id": "r", "strategy": { "fixed": { "month": 1, "day": 1 } } } ] }
          ]
        }
        """;

    /// <summary>
    /// Verifies that each supported <see cref="NotableDateCategory" /> token on a notable-date round-trips through both
    /// the XML and JSON loaders to the matching enum member.
    /// </summary>
    /// <param name="token">The category token to author.</param>
    /// <param name="expected">The expected resolved category.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("None", NotableDateCategory.None)]
    [DataRow("PublicHoliday", NotableDateCategory.PublicHoliday)]
    [DataRow("BankHoliday", NotableDateCategory.BankHoliday)]
    [DataRow("Observance", NotableDateCategory.Observance)]
    [DataRow("Remembrance", NotableDateCategory.Remembrance)]
    [DataRow("Cultural", NotableDateCategory.Cultural)]
    [DataRow("Religious", NotableDateCategory.Religious)]
    [DataRow("Seasonal", NotableDateCategory.Seasonal)]
    [DataRow("Civic", NotableDateCategory.Civic)]
    [DataRow("School", NotableDateCategory.School)]
    [DataRow("Regional", NotableDateCategory.Regional)]
    [DataRow("Other", NotableDateCategory.Other)]
    public void Load_WhenCategoryUsesSupportedValue_ShouldResolveExpectedCategory(string token, NotableDateCategory expected)
    {
        NotableDateResource xml = NotableDateResourceLoader.Load(CategoryXml(token));
        NotableDateResource json = NotableDateResourceLoader.LoadJson(CategoryJson(token));

        Assert.AreEqual(expected, xml.NotableDates.Single().Category, "XML category");
        Assert.AreEqual(expected, json.NotableDates.Single().Category, "JSON category");
    }

    /// <summary>
    /// Verifies that each supported <see cref="AdjustmentTrigger" /> token on an adjustment policy round-trips through
    /// both the XML and JSON loaders to the matching enum member. The <see cref="AdjustmentTrigger.Custom" /> arm carries
    /// the required handler key so the policy validates.
    /// </summary>
    /// <param name="token">The trigger token to author.</param>
    /// <param name="expected">The expected resolved trigger.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("Always", AdjustmentTrigger.Always)]
    [DataRow("IfDayOfWeek", AdjustmentTrigger.IfDayOfWeek)]
    [DataRow("IfWeekend", AdjustmentTrigger.IfWeekend)]
    [DataRow("IfWeekday", AdjustmentTrigger.IfWeekday)]
    [DataRow("IfLeapYear", AdjustmentTrigger.IfLeapYear)]
    [DataRow("IfBeforeFixedDate", AdjustmentTrigger.IfBeforeFixedDate)]
    [DataRow("IfAfterFixedDate", AdjustmentTrigger.IfAfterFixedDate)]
    [DataRow("IfNthOccurrenceInMonth", AdjustmentTrigger.IfNthOccurrenceInMonth)]
    [DataRow("Custom", AdjustmentTrigger.Custom)]
    public void Load_WhenTriggerUsesSupportedValue_ShouldResolveExpectedTrigger(string token, AdjustmentTrigger expected)
    {
        string handlerKey = token == "Custom" ? " handlerKey=\"k\"" : string.Empty;
        string xml =
            $"""
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.enum">
              <AdjustmentPolicies>
                <AdjustmentPolicy id="p"><Trigger type="{token}"{handlerKey} /><Action type="None" /><Emission mode="ActualOnly" /></AdjustmentPolicy>
              </AdjustmentPolicies>
              <NotableDates>
                <NotableDate id="x" displayName="X" category="PublicHoliday">
                  <Rules><Rule id="r"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        string jsonHandler = token == "Custom" ? ", \"handlerKey\": \"k\"" : string.Empty;
        string json =
            $$"""
            {
              "schemaVersion": "1.0", "resourceId": "test.enum",
              "adjustmentPolicies": [
                { "id": "p", "trigger": { "type": "{{token}}"{{jsonHandler}} }, "action": { "type": "None" }, "emission": { "mode": "ActualOnly" } }
              ],
              "notableDates": [
                { "id": "x", "displayName": "X", "category": "PublicHoliday",
                  "rules": [ { "id": "r", "strategy": { "fixed": { "month": 1, "day": 1 } } } ] }
              ]
            }
            """;

        Assert.AreEqual(expected, NotableDateResourceLoader.Load(xml).AdjustmentPolicies.Single().Trigger, "XML trigger");
        Assert.AreEqual(expected, NotableDateResourceLoader.LoadJson(json).AdjustmentPolicies.Single().Trigger, "JSON trigger");
    }

    /// <summary>
    /// Verifies that each supported <see cref="AdjustmentAction" /> token on an adjustment policy round-trips through
    /// both the XML and JSON loaders to the matching enum member. The <see cref="AdjustmentAction.Custom" /> arm carries
    /// the required handler key and the <see cref="AdjustmentAction.ReplaceWithRule" /> arm carries a resolvable
    /// reference so the policy validates.
    /// </summary>
    /// <param name="token">The action token to author.</param>
    /// <param name="expected">The expected resolved action.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("None", AdjustmentAction.None)]
    [DataRow("AddDays", AdjustmentAction.AddDays)]
    [DataRow("MoveToNextWeekday", AdjustmentAction.MoveToNextWeekday)]
    [DataRow("MoveToPreviousWeekday", AdjustmentAction.MoveToPreviousWeekday)]
    [DataRow("MoveToNextWorkingDay", AdjustmentAction.MoveToNextWorkingDay)]
    [DataRow("MoveToPreviousWorkingDay", AdjustmentAction.MoveToPreviousWorkingDay)]
    [DataRow("ReplaceWithRule", AdjustmentAction.ReplaceWithRule)]
    [DataRow("Suppress", AdjustmentAction.Suppress)]
    [DataRow("Custom", AdjustmentAction.Custom)]
    public void Load_WhenActionUsesSupportedValue_ShouldResolveExpectedAction(string token, AdjustmentAction expected)
    {
        string xmlAttrs = token switch
        {
            "Custom" => " handlerKey=\"k\"",
            "ReplaceWithRule" => " notableDateRef=\"x\" ruleRef=\"r\"",
            _ => string.Empty,
        };
        string xml =
            $"""
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.enum">
              <AdjustmentPolicies>
                <AdjustmentPolicy id="p"><Trigger type="Always" /><Action type="{token}"{xmlAttrs} /><Emission mode="ActualOnly" /></AdjustmentPolicy>
              </AdjustmentPolicies>
              <NotableDates>
                <NotableDate id="x" displayName="X" category="PublicHoliday">
                  <Rules><Rule id="r"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        string jsonAttrs = token switch
        {
            "Custom" => ", \"handlerKey\": \"k\"",
            "ReplaceWithRule" => ", \"notableDateRef\": \"x\", \"ruleRef\": \"r\"",
            _ => string.Empty,
        };
        string json =
            $$"""
            {
              "schemaVersion": "1.0", "resourceId": "test.enum",
              "adjustmentPolicies": [
                { "id": "p", "trigger": { "type": "Always" }, "action": { "type": "{{token}}"{{jsonAttrs}} }, "emission": { "mode": "ActualOnly" } }
              ],
              "notableDates": [
                { "id": "x", "displayName": "X", "category": "PublicHoliday",
                  "rules": [ { "id": "r", "strategy": { "fixed": { "month": 1, "day": 1 } } } ] }
              ]
            }
            """;

        Assert.AreEqual(expected, NotableDateResourceLoader.Load(xml).AdjustmentPolicies.Single().Action, "XML action");
        Assert.AreEqual(expected, NotableDateResourceLoader.LoadJson(json).AdjustmentPolicies.Single().Action, "JSON action");
    }

    /// <summary>
    /// Verifies that each supported <see cref="EmissionMode" /> token on an adjustment policy round-trips through both
    /// the XML and JSON loaders to the matching enum member.
    /// </summary>
    /// <param name="token">The emission-mode token to author.</param>
    /// <param name="expected">The expected resolved emission mode.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("ActualOnly", EmissionMode.ActualOnly)]
    [DataRow("ObservedOnly", EmissionMode.ObservedOnly)]
    [DataRow("ActualAndObserved", EmissionMode.ActualAndObserved)]
    [DataRow("ObservedAsAdditional", EmissionMode.ObservedAsAdditional)]
    [DataRow("Suppress", EmissionMode.Suppress)]
    public void Load_WhenEmissionUsesSupportedValue_ShouldResolveExpectedEmission(string token, EmissionMode expected)
    {
        string xml =
            $"""
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.enum">
              <AdjustmentPolicies>
                <AdjustmentPolicy id="p"><Trigger type="Always" /><Action type="None" /><Emission mode="{token}" /></AdjustmentPolicy>
              </AdjustmentPolicies>
              <NotableDates>
                <NotableDate id="x" displayName="X" category="PublicHoliday">
                  <Rules><Rule id="r"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        string json =
            $$"""
            {
              "schemaVersion": "1.0", "resourceId": "test.enum",
              "adjustmentPolicies": [
                { "id": "p", "trigger": { "type": "Always" }, "action": { "type": "None" }, "emission": { "mode": "{{token}}" } }
              ],
              "notableDates": [
                { "id": "x", "displayName": "X", "category": "PublicHoliday",
                  "rules": [ { "id": "r", "strategy": { "fixed": { "month": 1, "day": 1 } } } ] }
              ]
            }
            """;

        Assert.AreEqual(expected, NotableDateResourceLoader.Load(xml).AdjustmentPolicies.Single().Emission, "XML emission");
        Assert.AreEqual(expected, NotableDateResourceLoader.LoadJson(json).AdjustmentPolicies.Single().Emission, "JSON emission");
    }

    /// <summary>
    /// Verifies that each supported weekday token on a <c>DayOfWeekInMonth</c> strategy round-trips through both the XML
    /// and JSON loaders to the matching <see cref="System.DayOfWeek" /> member.
    /// </summary>
    /// <param name="token">The weekday token to author.</param>
    /// <param name="expected">The expected resolved weekday.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("Monday", DayOfWeek.Monday)]
    [DataRow("Tuesday", DayOfWeek.Tuesday)]
    [DataRow("Wednesday", DayOfWeek.Wednesday)]
    [DataRow("Thursday", DayOfWeek.Thursday)]
    [DataRow("Friday", DayOfWeek.Friday)]
    [DataRow("Saturday", DayOfWeek.Saturday)]
    [DataRow("Sunday", DayOfWeek.Sunday)]
    public void Load_WhenDayOfWeekInMonthUsesSupportedWeekday_ShouldResolveExpectedWeekday(string token, DayOfWeek expected)
    {
        string xml =
            $"""
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.enum">
              <NotableDates>
                <NotableDate id="x" displayName="X" category="Observance">
                  <Rules><Rule id="r"><Strategy><DayOfWeekInMonth month="May" dayOfWeek="{token}" weekOrdinal="First" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        string json =
            $$"""
            {
              "schemaVersion": "1.0", "resourceId": "test.enum",
              "notableDates": [
                { "id": "x", "displayName": "X", "category": "Observance",
                  "rules": [ { "id": "r", "strategy": { "dayOfWeekInMonth": { "month": "May", "dayOfWeek": "{{token}}", "weekOrdinal": "First" } } } ] }
              ]
            }
            """;

        DayOfWeekInMonthStrategy xmlStrategy = (DayOfWeekInMonthStrategy)NotableDateResourceLoader.Load(xml).NotableDates.Single().Rules.Single().Strategy;
        DayOfWeekInMonthStrategy jsonStrategy = (DayOfWeekInMonthStrategy)NotableDateResourceLoader.LoadJson(json).NotableDates.Single().Rules.Single().Strategy;

        Assert.AreEqual(expected, xmlStrategy.DayOfWeek, "XML dayOfWeek");
        Assert.AreEqual(expected, jsonStrategy.DayOfWeek, "JSON dayOfWeek");
    }

    /// <summary>
    /// Verifies that each supported <see cref="WeekOrdinal" /> token on a <c>DayOfWeekInMonth</c> strategy round-trips
    /// through both the XML and JSON loaders to the matching enum member.
    /// </summary>
    /// <param name="token">The ordinal token to author.</param>
    /// <param name="expected">The expected resolved ordinal.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("First", WeekOrdinal.First)]
    [DataRow("Second", WeekOrdinal.Second)]
    [DataRow("Third", WeekOrdinal.Third)]
    [DataRow("Fourth", WeekOrdinal.Fourth)]
    [DataRow("Fifth", WeekOrdinal.Fifth)]
    [DataRow("Last", WeekOrdinal.Last)]
    public void Load_WhenDayOfWeekInMonthUsesSupportedOrdinal_ShouldResolveExpectedOrdinal(string token, WeekOrdinal expected)
    {
        string xml =
            $"""
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.enum">
              <NotableDates>
                <NotableDate id="x" displayName="X" category="Observance">
                  <Rules><Rule id="r"><Strategy><DayOfWeekInMonth month="May" dayOfWeek="Monday" weekOrdinal="{token}" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        string json =
            $$"""
            {
              "schemaVersion": "1.0", "resourceId": "test.enum",
              "notableDates": [
                { "id": "x", "displayName": "X", "category": "Observance",
                  "rules": [ { "id": "r", "strategy": { "dayOfWeekInMonth": { "month": "May", "dayOfWeek": "Monday", "weekOrdinal": "{{token}}" } } } ] }
              ]
            }
            """;

        DayOfWeekInMonthStrategy xmlStrategy = (DayOfWeekInMonthStrategy)NotableDateResourceLoader.Load(xml).NotableDates.Single().Rules.Single().Strategy;
        DayOfWeekInMonthStrategy jsonStrategy = (DayOfWeekInMonthStrategy)NotableDateResourceLoader.LoadJson(json).NotableDates.Single().Rules.Single().Strategy;

        Assert.AreEqual(expected, xmlStrategy.WeekOrdinal, "XML weekOrdinal");
        Assert.AreEqual(expected, jsonStrategy.WeekOrdinal, "JSON weekOrdinal");
    }

    /// <summary>
    /// Verifies that each supported <see cref="WeekdayProximity" /> token on a <c>WeekdayNearDate</c> strategy
    /// round-trips through both the XML and JSON loaders to the matching enum member.
    /// </summary>
    /// <param name="token">The proximity token to author.</param>
    /// <param name="expected">The expected resolved proximity.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("Before", WeekdayProximity.Before)]
    [DataRow("OnOrBefore", WeekdayProximity.OnOrBefore)]
    [DataRow("Nearest", WeekdayProximity.Nearest)]
    [DataRow("OnOrAfter", WeekdayProximity.OnOrAfter)]
    [DataRow("After", WeekdayProximity.After)]
    public void Load_WhenWeekdayNearDateUsesSupportedProximity_ShouldResolveExpectedProximity(string token, WeekdayProximity expected)
    {
        string xml =
            $"""
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.enum">
              <NotableDates>
                <NotableDate id="x" displayName="X" category="PublicHoliday">
                  <Rules><Rule id="r"><Strategy><WeekdayNearDate month="May" day="24" dayOfWeek="Monday" direction="{token}" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        string json =
            $$"""
            {
              "schemaVersion": "1.0", "resourceId": "test.enum",
              "notableDates": [
                { "id": "x", "displayName": "X", "category": "PublicHoliday",
                  "rules": [ { "id": "r", "strategy": { "weekdayNearDate": { "month": "May", "day": 24, "dayOfWeek": "Monday", "direction": "{{token}}" } } } ] }
              ]
            }
            """;

        WeekdayNearDateStrategy xmlStrategy = (WeekdayNearDateStrategy)NotableDateResourceLoader.Load(xml).NotableDates.Single().Rules.Single().Strategy;
        WeekdayNearDateStrategy jsonStrategy = (WeekdayNearDateStrategy)NotableDateResourceLoader.LoadJson(json).NotableDates.Single().Rules.Single().Strategy;

        Assert.AreEqual(expected, xmlStrategy.Direction, "XML direction");
        Assert.AreEqual(expected, jsonStrategy.Direction, "JSON direction");
    }

    /// <summary>
    /// Verifies that omitting the optional <c>dayOfWeek</c> and <c>weekOrdinal</c> attributes on an adjustment policy
    /// leaves the corresponding nullable values unset rather than defaulting to the underlying zero member. Ported from
    /// the v1 <c>ParseJson_WhenAdjustmentOptionalEnumsAreOmitted_ShouldLeaveOptionalEnumsNull</c>.
    /// </summary>
    [TestMethod]
    public void Load_WhenAdjustmentOptionalEnumsAreOmitted_ShouldLeaveThemNull()
    {
        const string xml =
            """
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.enum">
              <AdjustmentPolicies>
                <AdjustmentPolicy id="p"><Trigger type="IfWeekend" /><Action type="MoveToNextWorkingDay" /><Emission mode="ObservedOnly" /></AdjustmentPolicy>
              </AdjustmentPolicies>
              <NotableDates>
                <NotableDate id="x" displayName="X" category="PublicHoliday">
                  <Rules><Rule id="r"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        AdjustmentPolicy policy = NotableDateResourceLoader.Load(xml).AdjustmentPolicies.Single();

        Assert.IsNull(policy.ActionWeekday, "actionWeekday unset");
        Assert.IsNull(policy.TriggerWeekOrdinal, "triggerWeekOrdinal unset");
    }
}
