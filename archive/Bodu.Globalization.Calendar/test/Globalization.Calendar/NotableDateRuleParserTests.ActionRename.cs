// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleParserTests.ActionRename.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that the XML parser maps the renamed <c>MoveToNextWorkingDay</c> action and still accepts the legacy
/// <c>MoveToNextNonWorkingDay</c> token as an alias (F-018.3).
/// </summary>
public partial class NotableDateRuleParserTests
{
    /// <summary>
    /// Verifies that the canonical <c>MoveToNextWorkingDay</c> action token maps to
    /// <see cref="AdjustmentAction.MoveToNextWorkingDay" />.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenActionUsesMoveToNextWorkingDay_ShouldMapToMoveToNextWorkingDay()
    {
        NotableDateRule rule = NotableDateRuleParser
            .ParseXml(BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""MoveToNextWorkingDay"""))
            .Single();

        Assert.AreEqual(AdjustmentAction.MoveToNextWorkingDay, rule.Adjustments.Single().Action);
    }

    /// <summary>
    /// Verifies that the legacy <c>MoveToNextNonWorkingDay</c> token is accepted and maps to the renamed
    /// <see cref="AdjustmentAction.MoveToNextWorkingDay" />.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenActionUsesLegacyMoveToNextNonWorkingDay_ShouldMapToMoveToNextWorkingDay()
    {
        NotableDateRule rule = NotableDateRuleParser
            .ParseXml(BuildAdjustmentXml(@"key=""t"" when=""Always"" action=""MoveToNextNonWorkingDay"""))
            .Single();

        Assert.AreEqual(AdjustmentAction.MoveToNextWorkingDay, rule.Adjustments.Single().Action);
    }
}
