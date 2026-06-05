// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleParserTests.DurationBounds.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using System.Xml.Schema;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that the XML schema rejects an out-of-range <c>durationDays</c> (F-016).
/// </summary>
public partial class NotableDateRuleParserTests
{
    /// <summary>
    /// Verifies that a <c>durationDays</c> greater than 366 is rejected by XSD validation.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenDurationDaysExceedsMaximum_ShouldThrowExactly()
    {
        const string xml = @"<NotableDates xmlns=""urn:bodu:globalization:calendar"">
            <NotableDate name=""T"">
                <Rule name=""R"" category=""Holiday"" durationDays=""400"">
                    <Fixed month=""January"" day=""1"" />
                </Rule>
            </NotableDate>
        </NotableDates>";

        Assert.ThrowsExactly<XmlSchemaValidationException>(() =>
        {
            _ = NotableDateRuleParser.ParseXml(xml);
        });
    }
}

/// <summary>
/// Verifies that the JSON schema rejects an out-of-range <c>durationDays</c> (F-016).
/// </summary>
public partial class NotableDateRuleJsonParserTests
{
    /// <summary>
    /// Verifies that a <c>durationDays</c> greater than 366 is rejected by JSON-schema validation.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenDurationDaysExceedsMaximum_ShouldThrowExactly()
    {
        const string json = @"{ ""notableDates"": [ { ""name"": ""T"", ""rules"": [ {
            ""name"": ""R"", ""category"": ""Holiday"", ""durationDays"": 400,
            ""fixed"": { ""month"": ""January"", ""day"": 1 }
        } ] } ] }";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = NotableDateRuleJsonParser.ParseJson(json);
        });
    }
}
