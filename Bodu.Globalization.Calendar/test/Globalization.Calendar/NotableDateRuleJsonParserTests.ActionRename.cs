// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleJsonParserTests.ActionRename.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that the JSON parser maps the renamed <c>MoveToNextWorkingDay</c> action and still accepts the legacy
/// <c>MoveToNextNonWorkingDay</c> token as an alias (F-018.3).
/// </summary>
public partial class NotableDateRuleJsonParserTests
{
    /// <summary>
    /// Builds a minimal document with a single adjustment whose action token is supplied.
    /// </summary>
    /// <param name="action">The action token to place on the adjustment.</param>
    /// <returns>The JSON document text.</returns>
    private static string BuildActionJson(string action) =>
        $@"{{ ""notableDates"": [ {{ ""name"": ""T"", ""rules"": [ {{
            ""name"": ""R"", ""category"": ""Holiday"",
            ""fixed"": {{ ""month"": ""January"", ""day"": 1 }},
            ""adjustments"": [ {{ ""key"": ""k"", ""when"": ""Always"", ""action"": ""{action}"" }} ]
        }} ] }} ] }}";

    /// <summary>
    /// Verifies that the canonical <c>MoveToNextWorkingDay</c> action token maps to
    /// <see cref="AdjustmentAction.MoveToNextWorkingDay" />.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenActionUsesMoveToNextWorkingDay_ShouldMapToMoveToNextWorkingDay()
    {
        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(BuildActionJson("MoveToNextWorkingDay")).Single();

        Assert.AreEqual(AdjustmentAction.MoveToNextWorkingDay, rule.Adjustments.Single().Action);
    }

    /// <summary>
    /// Verifies that the legacy <c>MoveToNextNonWorkingDay</c> token is accepted and maps to the renamed
    /// <see cref="AdjustmentAction.MoveToNextWorkingDay" />.
    /// </summary>
    [TestMethod]
    public void ParseJson_WhenActionUsesLegacyMoveToNextNonWorkingDay_ShouldMapToMoveToNextWorkingDay()
    {
        NotableDateRule rule = NotableDateRuleJsonParser.ParseJson(BuildActionJson("MoveToNextNonWorkingDay")).Single();

        Assert.AreEqual(AdjustmentAction.MoveToNextWorkingDay, rule.Adjustments.Single().Action);
    }
}
