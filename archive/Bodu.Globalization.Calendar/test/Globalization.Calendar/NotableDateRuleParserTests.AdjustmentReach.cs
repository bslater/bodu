// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleParserTests.AdjustmentReach.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that the XML parser exposes the adjustment reach envelope and custom-handler parameters (F-010).
/// </summary>
public partial class NotableDateRuleParserTests
{
    /// <summary>
    /// Verifies that the XML parser maps the <c>maxReachDays</c> attribute to
    /// <see cref="ObservanceAdjustment.MaxAdjustmentReachDays" />.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenAdjustmentDeclaresMaxReachDays_ShouldMapMaxAdjustmentReachDays()
    {
        var xml = @"<NotableDates xmlns=""urn:bodu:globalization:calendar"">
            <NotableDate name=""Test"">
                <Rule name=""Test Rule"" category=""Holiday"">
                    <Fixed month=""January"" day=""1"" />
                    <Adjustment key=""roll"" when=""Always"" action=""None"" maxReachDays=""45"" />
                </Rule>
            </NotableDate>
        </NotableDates>";

        NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();

        Assert.AreEqual(45, rule.Adjustments.Single().MaxAdjustmentReachDays);
    }

    /// <summary>
    /// Verifies that the XML parser maps repeated <c>&lt;Param&gt;</c> child elements to
    /// <see cref="ObservanceAdjustment.HandlerParameters" />.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenAdjustmentDeclaresParamChildren_ShouldMapHandlerParameters()
    {
        var xml = @"<NotableDates xmlns=""urn:bodu:globalization:calendar"">
            <NotableDate name=""Test"">
                <Rule name=""Test Rule"" category=""Holiday"">
                    <Fixed month=""January"" day=""1"" />
                    <Adjustment key=""custom"" when=""Custom"" action=""Custom"" handlerKey=""my-handler"">
                        <Param key=""shift"" value=""3"" />
                        <Param key=""mode"" value=""forward"" />
                    </Adjustment>
                </Rule>
            </NotableDate>
        </NotableDates>";

        NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();
        IReadOnlyDictionary<string, string>? parameters = rule.Adjustments.Single().HandlerParameters;

        Assert.IsNotNull(parameters);
        Assert.AreEqual("3", parameters["shift"]);
        Assert.AreEqual("forward", parameters["mode"]);
    }
}
