// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleParserTests.AlgorithmType.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDateRuleParser" /> handles the optional <c>type</c> attribute on
/// <c>&lt;Algorithm&gt;</c> elements across the three branches of <c>ParseOptionalType&lt;TBase&gt;</c>:
/// attribute absent, attribute present but unresolvable, attribute resolves but does not implement the contract,
/// attribute resolves and implements the contract.
/// </summary>
public partial class NotableDateRuleParserTests
{
    /// <summary>
    /// Verifies that omitting the <c>type</c> attribute on an Algorithm element leaves
    /// <see cref="NotableDateRule.AlgorithmType" /> at <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenAlgorithmOmitsTypeAttribute_ShouldLeaveAlgorithmTypeNull()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Easter Sunday"">
					<Rule name=""Easter Sunday Rule"" category=""Religious"" firstYear=""1583"">
						<Algorithm key=""easter-sunday"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

        NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();

        Assert.AreEqual("easter-sunday", rule.AlgorithmKey);
        Assert.IsNull(rule.AlgorithmType);
    }

    /// <summary>
    /// Verifies that a <c>type</c> attribute whose value cannot be resolved by <see cref="Type.GetType(string, bool)" />
    /// leaves <see cref="NotableDateRule.AlgorithmType" /> at <see langword="null" /> (no exception is surfaced).
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenAlgorithmTypeIsUnresolvable_ShouldLeaveAlgorithmTypeNull()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Phantom Algorithm"">
					<Rule name=""Phantom Algorithm Rule"" category=""Observance"">
						<Algorithm type=""NonExistent.Phantom.Algorithm"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

        NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();

        Assert.IsNull(rule.AlgorithmType);
    }

    /// <summary>
    /// Verifies that a <c>type</c> attribute whose value resolves to a CLR type that does not implement
    /// <see cref="INotableDateAlgorithm" /> leaves <see cref="NotableDateRule.AlgorithmType" /> at <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenAlgorithmTypeDoesNotImplementContract_ShouldLeaveAlgorithmTypeNull()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Wrong Contract"">
					<Rule name=""Wrong Contract Rule"" category=""Observance"">
						<Algorithm type=""System.String"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

        NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();

        Assert.IsNull(rule.AlgorithmType);
    }

    /// <summary>
    /// Verifies that an Algorithm element with the optional <c>month</c> and <c>day</c> attributes propagates them
    /// onto <see cref="NotableDateRule.AlgorithmMonth" /> and <see cref="NotableDateRule.AlgorithmDay" />.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenAlgorithmSuppliesMonthAndDay_ShouldPopulateAlgorithmMonthAndDay()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Festival"">
					<Rule name=""Festival Rule"" category=""Cultural"">
						<Algorithm key=""configurable"" month=""March"" day=""17"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

        NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();

        Assert.AreEqual("March", rule.AlgorithmMonth);
        Assert.AreEqual(17, rule.AlgorithmDay);
    }

    /// <summary>
    /// Verifies that an Adjustment element's optional <c>comparisonMonth</c> / <c>comparisonDay</c> attributes — when
    /// authored with a non-numeric day value (defeating <see cref="int.TryParse(string?, out int)" />) — yield no
    /// <see cref="ObservanceAdjustment.ComparisonDate" />. This is reachable only by bypassing schema validation
    /// because the XSD restricts <c>comparisonDay</c> to the <c>day</c> simple type; the test routes through the
    /// in-memory <see cref="System.Xml.Linq.XDocument" /> overload which validates against the same schema, so we
    /// assert the schema-level rejection rather than the parser-level fallback.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenAdjustmentComparisonDayIsNonNumeric_ShouldThrowExactly()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Bad Comparison"">
					<Rule name=""Bad Comparison Rule"" category=""Holiday"">
						<Fixed month=""January"" day=""1"" />
						<Adjustment key=""bad"" when=""IfBeforeFixedDate"" action=""MoveToNextWeekday"" comparisonMonth=""March"" comparisonDay=""banana"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

        Assert.ThrowsExactly<System.Xml.Schema.XmlSchemaValidationException>(() =>
        {
            _ = NotableDateRuleParser.ParseXml(xml);
        });
    }

    /// <summary>
    /// Verifies that a top-level Rule with two Adjustment children sharing the same <c>key</c> attribute surfaces as
    /// <see cref="InvalidOperationException" /> from <c>EnsureUniqueAdjustmentKeys</c>, mentioning the duplicate key.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenStandaloneRuleHasDuplicateAdjustmentKeys_ShouldThrowExactly()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Duplicate"">
					<Rule name=""Duplicate Rule"" category=""Holiday"">
						<Fixed month=""January"" day=""1"" />
						<Adjustment key=""roll"" when=""IfWeekend"" action=""MoveToNextWeekday"" />
						<Adjustment key=""roll"" when=""Always"" action=""None"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

        InvalidOperationException ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = NotableDateRuleParser.ParseXml(xml);
        });

        Assert.IsTrue(ex.Message.Contains("roll", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that an Adjustment element with the full optional surface (<c>dayOfWeek</c>, <c>weekOrdinal</c>,
    /// <c>days</c>, <c>priority</c>, <c>nonWorking</c>, <c>territory</c>, <c>fromYear</c>, <c>toYear</c>,
    /// <c>comparisonMonth</c>, <c>comparisonDay</c>, <c>target</c>, <c>handlerKey</c>) propagates each field onto
    /// the resulting <see cref="ObservanceAdjustment" />.
    /// </summary>
    [TestMethod]
    public void ParseXml_WhenAdjustmentSuppliesFullOptionalSurface_ShouldMapAllFields()
    {
        const string xml = @"
			<NotableDates xmlns=""urn:bodu:globalization:calendar"">
				<NotableDate name=""Comprehensive"">
					<Rule name=""Comprehensive Rule"" category=""Holiday"">
						<Fixed month=""December"" day=""26"" />
						<Adjustment
							key=""bk-day-roll""
							when=""IfNthOccurrenceInMonth""
							action=""AddDays""
							dayOfWeek=""Monday""
							weekOrdinal=""Second""
							days=""2""
							priority=""10""
							nonWorking=""true""
							territory=""AU""
							fromYear=""2000""
							toYear=""2099""
							comparisonMonth=""December""
							comparisonDay=""25""
							target=""Christmas Day""
							handlerKey=""bank-holiday"" />
					</Rule>
				</NotableDate>
			</NotableDates>";

        NotableDateRule rule = NotableDateRuleParser.ParseXml(xml).Single();
        ObservanceAdjustment adjustment = rule.Adjustments.Single();

        Assert.AreEqual("bk-day-roll", adjustment.Key);
        Assert.AreEqual(AdjustmentTrigger.IfNthOccurrenceInMonth, adjustment.Trigger);
        Assert.AreEqual(AdjustmentAction.AddDays, adjustment.Action);
        Assert.AreEqual(DayOfWeek.Monday, adjustment.DayOfWeek);
        Assert.AreEqual(WeekOfMonthOrdinal.Second, adjustment.WeekOrdinal);
        Assert.AreEqual(2, adjustment.OffsetDays);
        Assert.AreEqual(10, adjustment.Priority);
        Assert.AreEqual(true, adjustment.IsNonWorkingDay);
        Assert.AreEqual("AU", adjustment.TerritoryCode);
        Assert.AreEqual(2000, adjustment.EffectiveFromYear);
        Assert.AreEqual(2099, adjustment.EffectiveToYear);
        Assert.AreEqual(12, adjustment.ComparisonDate!.Value.Month);
        Assert.AreEqual(25, adjustment.ComparisonDate.Value.Day);
        Assert.AreEqual("Christmas Day", adjustment.TargetRuleName);
        Assert.AreEqual("bank-holiday", adjustment.HandlerKey);
    }
}
