// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDefinitionParserTests.ParseXml.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Collections.Immutable;
using System.Xml.Linq;

namespace Bodu.Globalization.Calendar
{
	public partial class NotableDateDefinitionParserTests
	{
		/// <summary>
		/// Verifies that parsing a valid complex XML document returns the full set of expected notable date definitions in the correct order.
		/// </summary>
		[TestMethod]
		public void ParseXml_WhenGivenValidComplexXml_ShouldReturnExpectedNotableDates()
		{
			var xmlDoc = XDocument.Parse(NotableDateDefinitionParserTests.ComplexNotableDatesXml);
			var expected = NotableDateDefinitionParserTests.GetComplexExpectedDefinitions().ToImmutableArray();

			var actual = NotableDateDefinitionParser.ParseXml(xmlDoc).OrderBy(d => d.Name).ToImmutableArray();

			Assert.AreEqual(expected, actual);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date XML definition populates the <c>Name</c> property.
		/// </summary>
		[TestMethod]
		public void ParseXml_FixedDefinition_ShouldPopulateName()
		{
			var doc = XDocument.Parse(NotableDateDefinitionParserTests.FixedDateXml);
			var definition = NotableDateDefinitionParser.ParseXml(doc).Single();

			Assert.AreEqual("Fixed Date Test", definition.Name);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date XML definition populates the <c>FirstYear</c> property.
		/// </summary>
		[TestMethod]
		public void ParseXml_FixedDefinition_ShouldPopulateFirstYear()
		{
			var doc = XDocument.Parse(NotableDateDefinitionParserTests.FixedDateXml);
			var definition = NotableDateDefinitionParser.ParseXml(doc).Single();

			Assert.AreEqual(2000, definition.FirstYear);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date XML definition populates the <c>LastYear</c> property.
		/// </summary>
		[TestMethod]
		public void ParseXml_FixedDefinition_ShouldPopulateLastYear()
		{
			var doc = XDocument.Parse(NotableDateDefinitionParserTests.FixedDateXml);
			var definition = NotableDateDefinitionParser.ParseXml(doc).Single();

			Assert.AreEqual(2100, definition.LastYear);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date XML definition populates the <c>Country</c> property.
		/// </summary>
		[TestMethod]
		public void ParseXml_FixedDefinition_ShouldPopulateCountry()
		{
			var doc = XDocument.Parse(NotableDateDefinitionParserTests.FixedDateXml);
			var definition = NotableDateDefinitionParser.ParseXml(doc).Single();

			Assert.AreEqual("AU", definition.Country);
		}

		/// <summary>
		/// Verifies that parsing a dynamic XML definition populates the <c>ProviderTypeName</c> property.
		/// </summary>
		[TestMethod]
		public void ParseXml_DynamicDefinition_ShouldPopulateProviderTypeName()
		{
			var doc = XDocument.Parse(NotableDateDefinitionParserTests.DynamicDateXml);
			var definition = NotableDateDefinitionParser.ParseXml(doc).Single();

			Assert.AreEqual("Bodu.Globalization.Calendar.Calculators.EasterSundayNotableDateCalculator", definition.ProviderTypeName);
		}

		/// <summary>
		/// Verifies that parsing a dynamic XML definition populates the <c>NotableDateCalculatorType</c> property.
		/// </summary>
		[TestMethod]
		public void ParseXml_DynamicDefinition_ShouldPopulateProviderAssembly()
		{
			var doc = XDocument.Parse(NotableDateDefinitionParserTests.DynamicDateXml);
			var definition = NotableDateDefinitionParser.ParseXml(doc).Single();

			Assert.AreEqual("Bodu.Globalization.Calendar", definition.NotableDateCalculatorType);
		}

		/// <summary>
		/// Verifies that parsing a rule-based XML definition populates the <c>DayOfWeek</c> property.
		/// </summary>
		[TestMethod]
		public void ParseXml_RuleBasedDefinition_ShouldPopulateDayOfWeek()
		{
			var doc = XDocument.Parse(NotableDateDefinitionParserTests.RuleBasedDateXml);
			var definition = NotableDateDefinitionParser.ParseXml(doc).Single();

			Assert.AreEqual(DayOfWeek.Monday, definition.DayOfWeek);
		}

		/// <summary>
		/// Verifies that parsing a rule-based XML definition populates the <c>WeekOrdinal</c> property.
		/// </summary>
		[TestMethod]
		public void ParseXml_RuleBasedDefinition_ShouldPopulateWeekOrdinal()
		{
			var doc = XDocument.Parse(NotableDateDefinitionParserTests.RuleBasedDateXml);
			var definition = NotableDateDefinitionParser.ParseXml(doc).Single();

			Assert.AreEqual(WeekOfMonthOrdinal.Second, definition.WeekOrdinal);
		}

		/// <summary>
		/// Verifies that parsing an offset-from XML definition populates the <c>BaseNotableDateName</c> property.
		/// </summary>
		[TestMethod]
		public void ParseXml_OffsetFromDefinition_ShouldPopulateBaseNotableDateName()
		{
			var doc = XDocument.Parse(NotableDateDefinitionParserTests.OffsetFromDateXml);
			var definition = NotableDateDefinitionParser.ParseXml(doc).Single();

			Assert.AreEqual("Easter Sunday", definition.BaseNotableDateName);
		}

		/// <summary>
		/// Verifies that parsing an offset-from XML definition populates the <c>OffsetDays</c> property.
		/// </summary>
		[TestMethod]
		public void ParseXml_OffsetFromDefinition_ShouldPopulateOffsetDays()
		{
			var doc = XDocument.Parse(NotableDateDefinitionParserTests.OffsetFromDateXml);
			var definition = NotableDateDefinitionParser.ParseXml(doc).Single();

			Assert.AreEqual(-2, definition.OffsetDays);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date XML definition sets the <c>DefinitionType</c> to <see cref="NotableDateDefinitionType.Fixed" />.
		/// </summary>
		[TestMethod]
		public void ParseXml_FixedDefinition_ShouldPopulateDefinitionType()
		{
			var doc = XDocument.Parse(NotableDateDefinitionParserTests.FixedDateXml);
			var definition = NotableDateDefinitionParser.ParseXml(doc).Single();

			Assert.AreEqual(NotableDateDefinitionType.Fixed, definition.DefinitionType);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date XML definition populates the <c>NotableDateKind</c> property.
		/// </summary>
		[TestMethod]
		public void ParseXml_FixedDefinition_ShouldPopulateNotableDateKind()
		{
			var doc = XDocument.Parse(NotableDateDefinitionParserTests.FixedDateXml);
			var definition = NotableDateDefinitionParser.ParseXml(doc).Single();

			Assert.AreEqual(NotableDateKind.Holiday, definition.NotableDateKind);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date XML definition populates the <c>NonWorking</c> flag.
		/// </summary>
		[TestMethod]
		public void ParseXml_FixedDefinition_ShouldPopulateNonWorking()
		{
			var doc = XDocument.Parse(NotableDateDefinitionParserTests.FixedDateXml);
			var definition = NotableDateDefinitionParser.ParseXml(doc).Single();

			Assert.IsTrue(definition.NonWorking ?? false);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date XML definition populates both the <c>Day</c> and <c>Month</c> properties.
		/// </summary>
		[TestMethod]
		public void ParseXml_FixedDefinition_ShouldPopulateDayAndMonth()
		{
			var doc = XDocument.Parse(NotableDateDefinitionParserTests.FixedDateXml);
			var definition = NotableDateDefinitionParser.ParseXml(doc).Single();

			Assert.AreEqual(1, definition.Day);
			Assert.AreEqual(1, definition.Month);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date XML definition populates the <c>Calendar</c> property with the expected calendar type name.
		/// </summary>
		[TestMethod]
		public void ParseXml_FixedDefinition_ShouldPopulateCalendarType()
		{
			var doc = XDocument.Parse(NotableDateDefinitionParserTests.FixedDateXml);
			var definition = NotableDateDefinitionParser.ParseXml(doc).Single();

			Assert.AreEqual("System.Globalization.GregorianCalendar", definition.Calendar);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date XML definition populates the <c>Region</c> property.
		/// </summary>
		[TestMethod]
		public void ParseXml_FixedDefinition_ShouldPopulateRegion()
		{
			var doc = XDocument.Parse(NotableDateDefinitionParserTests.FixedDateXml);
			var definition = NotableDateDefinitionParser.ParseXml(doc).Single();

			Assert.AreEqual("NSW", definition.Region);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date XML definition populates the <c>OccurrenceYears</c> property.
		/// </summary>
		[TestMethod]
		public void ParseXml_FixedDefinition_ShouldPopulateOccurrenceYears()
		{
			var doc = XDocument.Parse(NotableDateDefinitionParserTests.FixedDateXml);
			var definition = NotableDateDefinitionParser.ParseXml(doc).Single();

			Assert.AreEqual(4, definition.OccurrenceYears);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date XML definition populates the <c>Comment</c> property.
		/// </summary>
		[TestMethod]
		public void ParseXml_FixedDefinition_ShouldPopulateComment()
		{
			var doc = XDocument.Parse(NotableDateDefinitionParserTests.FixedDateXml);
			var definition = NotableDateDefinitionParser.ParseXml(doc).Single();

			Assert.AreEqual("Fixed date comment.", definition.Comment);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date XML definition correctly parses the embedded adjustment rules collection.
		/// </summary>
		[TestMethod]
		public void ParseXml_FixedDefinition_ShouldParseAdjustmentRules()
		{
			var doc = XDocument.Parse(NotableDateDefinitionParserTests.FixedDateXml);
			var definition = NotableDateDefinitionParser.ParseXml(doc).Single();

			Assert.AreEqual(1, definition.AdjustmentRules.Count());
			Assert.AreEqual(NotableDateAdjustmentRuleType.IfWeekend, definition.AdjustmentRules[0].AdjustmentRule);
			Assert.AreEqual(NotableDateAdjustmentActionType.MoveToNextWeekday, definition.AdjustmentRules[0].Action);
		}
	}
}
