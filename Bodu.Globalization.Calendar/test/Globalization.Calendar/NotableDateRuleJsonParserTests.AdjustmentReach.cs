// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleJsonParserTests.AdjustmentReach.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that the JSON parser exposes the adjustment reach envelope and custom-handler parameters (F-010).
/// </summary>
public partial class NotableDateRuleJsonParserTests
{
    /// <summary>
    /// Verifies that the JSON parser maps the <c>maxReachDays</c> property to
    /// <see cref="ObservanceAdjustment.MaxAdjustmentReachDays" />.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenAdjustmentDeclaresMaxReachDays_ShouldMapMaxAdjustmentReachDays()
    {
        var json = @"{ ""notableDates"": [ { ""name"": ""Test"", ""rules"": [ {
            ""name"": ""Test Rule"", ""category"": ""Holiday"",
            ""fixed"": { ""month"": ""January"", ""day"": 1 },
            ""adjustments"": [ { ""key"": ""roll"", ""when"": ""Always"", ""action"": ""None"", ""maxReachDays"": 45 } ]
        } ] } ] }";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();

        Assert.AreEqual(45, rule.Adjustments.Single().MaxAdjustmentReachDays);
    }

    /// <summary>
    /// Verifies that the JSON parser maps the <c>handlerParameters</c> object to
    /// <see cref="ObservanceAdjustment.HandlerParameters" />.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenAdjustmentDeclaresHandlerParameters_ShouldMapHandlerParameters()
    {
        var json = @"{ ""notableDates"": [ { ""name"": ""Test"", ""rules"": [ {
            ""name"": ""Test Rule"", ""category"": ""Holiday"",
            ""fixed"": { ""month"": ""January"", ""day"": 1 },
            ""adjustments"": [ { ""key"": ""custom"", ""when"": ""Custom"", ""action"": ""Custom"", ""handlerKey"": ""my-handler"",
                ""handlerParameters"": { ""shift"": ""3"", ""mode"": ""forward"" } } ]
        } ] } ] }";

        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(json).Single();
        IReadOnlyDictionary<string, string>? parameters = rule.Adjustments.Single().HandlerParameters;

        Assert.IsNotNull(parameters);
        Assert.AreEqual("3", parameters["shift"]);
        Assert.AreEqual("forward", parameters["mode"]);
    }
}
