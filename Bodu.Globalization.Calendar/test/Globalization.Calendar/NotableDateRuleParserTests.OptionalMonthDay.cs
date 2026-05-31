// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleParserTests.OptionalMonthDay.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the optional <c>comparisonMonth</c> / <c>comparisonDay</c> handling of <see cref="NotableDateRuleParser" />
/// in three configurations: both absent, only month present, only day present. Each leaves
/// <see cref="ObservanceAdjustment.ComparisonDate" /> at <see langword="null" /> because the parser requires both
/// to construct the synthetic comparison date.
/// </summary>
public partial class NotableDateRuleParserTests
{
    /// <summary>
    /// Verifies that an Adjustment whose <c>comparisonMonth</c> attribute is supplied without a matching
    /// <c>comparisonDay</c> leaves <see cref="ObservanceAdjustment.ComparisonDate" /> at <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenAdjustmentSuppliesOnlyComparisonMonth_ShouldLeaveComparisonDateNull()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Lone Month"">
					<Rule name=""Lone Month Rule"" category=""Holiday"">
						<Fixed month=""December"" day=""26"" />
						<Adjustment key=""half"" when=""IfWeekend"" action=""MoveToNextWeekday"" comparisonMonth=""December"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

        NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();

        Assert.IsNull(rule.Adjustments[0].ComparisonDate);
    }

    /// <summary>
    /// Verifies that an Adjustment whose <c>comparisonDay</c> attribute is supplied without a matching
    /// <c>comparisonMonth</c> leaves <see cref="ObservanceAdjustment.ComparisonDate" /> at <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenAdjustmentSuppliesOnlyComparisonDay_ShouldLeaveComparisonDateNull()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Lone Day"">
					<Rule name=""Lone Day Rule"" category=""Holiday"">
						<Fixed month=""December"" day=""26"" />
						<Adjustment key=""half"" when=""IfWeekend"" action=""MoveToNextWeekday"" comparisonDay=""15"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

        NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();

        Assert.IsNull(rule.Adjustments[0].ComparisonDate);
    }

    /// <summary>
    /// Verifies that an Adjustment omitting both <c>comparisonMonth</c> and <c>comparisonDay</c> leaves
    /// <see cref="ObservanceAdjustment.ComparisonDate" /> at <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenAdjustmentOmitsBothComparisonAttributes_ShouldLeaveComparisonDateNull()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""No Comparison"">
					<Rule name=""No Comparison Rule"" category=""Holiday"">
						<Fixed month=""December"" day=""26"" />
						<Adjustment key=""half"" when=""IfWeekend"" action=""MoveToNextWeekday"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

        NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();

        Assert.IsNull(rule.Adjustments[0].ComparisonDate);
    }
}
