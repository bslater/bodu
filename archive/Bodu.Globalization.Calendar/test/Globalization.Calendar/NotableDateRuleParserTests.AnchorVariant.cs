// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleParserTests.AnchorVariant.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that the XML parser reads the variant-disambiguating anchor and replacement-target attributes so a
/// cookbook author can bind an offset rule or <see cref="AdjustmentAction.ReplaceWithNamedDate" /> adjustment to a
/// specific rule variant rather than the first same-name candidate.
/// </summary>
public partial class NotableDateRuleParserTests
{
    /// <summary>
    /// Verifies that the XML parser maps the optional <c>anchorRuleVariant</c>, <c>anchorTerritory</c>, and
    /// <c>anchorCalendarType</c> attributes of an <c>&lt;OffsetFromAnchor&gt;</c> element onto the rule.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenOffsetAnchorDeclaresVariant_ShouldMapAnchorReferenceFields()
    {
        var xml = @"<NotableDates xmlns=""urn:bodu:globalization:calendar"">
            <NotableDate name=""Good Friday"">
                <Rule name=""Good Friday"" category=""Holiday"">
                    <OffsetFromAnchor name=""Easter Sunday"" offset=""-2"" anchorRuleVariant=""western""
                        anchorTerritory=""US"" anchorCalendarType=""System.Globalization.HijriCalendar"" />
                </Rule>
            </NotableDate>
        </NotableDates>";

        NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();

        Assert.AreEqual("Easter Sunday", rule.AnchorRuleName);
        Assert.AreEqual(-2, rule.OffsetDays);
        Assert.AreEqual("western", rule.AnchorRuleVariant);
        Assert.AreEqual("US", rule.AnchorTerritoryCode);
        Assert.AreEqual(typeof(HijriCalendar), rule.AnchorCalendarType);
    }

    /// <summary>
    /// Verifies that the XML parser leaves the anchor reference fields <see langword="null" /> when only the required
    /// <c>name</c> and <c>offset</c> attributes are authored, preserving backward compatibility.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenOffsetAnchorOmitsVariant_ShouldLeaveAnchorReferenceFieldsNull()
    {
        var xml = @"<NotableDates xmlns=""urn:bodu:globalization:calendar"">
            <NotableDate name=""Good Friday"">
                <Rule name=""Good Friday"" category=""Holiday"">
                    <OffsetFromAnchor name=""Easter Sunday"" offset=""-2"" />
                </Rule>
            </NotableDate>
        </NotableDates>";

        NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();

        Assert.AreEqual("Easter Sunday", rule.AnchorRuleName);
        Assert.IsNull(rule.AnchorRuleVariant);
        Assert.IsNull(rule.AnchorTerritoryCode);
        Assert.IsNull(rule.AnchorCalendarType);
    }

    /// <summary>
    /// Verifies that the XML parser maps the optional <c>targetRuleVariant</c> attribute of a
    /// <c>ReplaceWithNamedDate</c> adjustment onto <see cref="ObservanceAdjustment.TargetRuleVariant" />.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenReplacementAdjustmentDeclaresTargetVariant_ShouldMapTargetRuleVariant()
    {
        var xml = @"<NotableDates xmlns=""urn:bodu:globalization:calendar"">
            <NotableDate name=""Easter Monday"">
                <Rule name=""Easter Monday"" category=""Holiday"">
                    <Fixed month=""January"" day=""1"" />
                    <Adjustment key=""sub"" when=""Always"" action=""ReplaceWithNamedDate"" target=""Easter Monday"" targetRuleVariant=""western"" />
                </Rule>
            </NotableDate>
        </NotableDates>";

        NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();

        Assert.AreEqual("Easter Monday", rule.Adjustments.Single().TargetRuleName);
        Assert.AreEqual("western", rule.Adjustments.Single().TargetRuleVariant);
    }
}
