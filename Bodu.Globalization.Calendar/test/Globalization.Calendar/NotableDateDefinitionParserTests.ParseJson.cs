// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDefinitionParserTests.ParseJson.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.Globalization.Calendar
{
	public partial class NotableDateDefinitionParserTests
	{
		/// <summary>
		/// Verifies that parsing a valid complex JSON document returns the full set of expected notable date definitions in the correct order.
		/// </summary>
		[TestMethod]
		public void ParseJson_WhenGivenValidComplexJson_ShouldReturnExpectedNotableDates()
		{
			var jsonString = NotableDateDefinitionParserTests.ComplexNotableDatesJson;
			var expected = NotableDateDefinitionParserTests.GetComplexExpectedDefinitions();

			var actual = NotableDateDefinitionParser.ParseJson(jsonString).OrderBy(d => d.Name).ToList();

			Assert.AreEqual(expected.Count, actual.Count);

			for (int i = 0; i < expected.Count; i++)
			{
				Assert.AreEqual(expected[i], actual[i]);
			}
		}

		/// <summary>
		/// Verifies that parsing a fixed-date JSON definition populates the <c>Name</c> property.
		/// </summary>
		[TestMethod]
		public void ParseJson_FixedDefinition_ShouldPopulateName()
		{
			var json = NotableDateDefinitionParserTests.FixedDateJson;
			var definition = NotableDateDefinitionParser.ParseJson(json).Single();

			Assert.AreEqual("Fixed Date Test", definition.Name);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date JSON definition populates the <c>FirstYear</c> property.
		/// </summary>
		[TestMethod]
		public void ParseJson_FixedDefinition_ShouldPopulateFirstYear()
		{
			var json = NotableDateDefinitionParserTests.FixedDateJson;
			var definition = NotableDateDefinitionParser.ParseJson(json).Single();

			Assert.AreEqual(2000, definition.FirstYear);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date JSON definition populates the <c>LastYear</c> property.
		/// </summary>
		[TestMethod]
		public void ParseJson_FixedDefinition_ShouldPopulateLastYear()
		{
			var json = NotableDateDefinitionParserTests.FixedDateJson;
			var definition = NotableDateDefinitionParser.ParseJson(json).Single();

			Assert.AreEqual(2100, definition.LastYear);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date JSON definition populates the <c>Country</c> property.
		/// </summary>
		[TestMethod]
		public void ParseJson_FixedDefinition_ShouldPopulateCountry()
		{
			var json = NotableDateDefinitionParserTests.FixedDateJson;
			var definition = NotableDateDefinitionParser.ParseJson(json).Single();

			Assert.AreEqual("AU", definition.Country);
		}

		/// <summary>
		/// Verifies that parsing a dynamic JSON definition populates the <c>ProviderTypeName</c> property.
		/// </summary>
		[TestMethod]
		public void ParseJson_DynamicDefinition_ShouldPopulateProviderTypeName()
		{
			var json = NotableDateDefinitionParserTests.DynamicDateJson;
			var definition = NotableDateDefinitionParser.ParseJson(json).Single();

			Assert.AreEqual("Bodu.Globalization.Calendar.Calculators.EasterSundayNotableDateCalculator", definition.ProviderTypeName);
		}

		/// <summary>
		/// Verifies that parsing a dynamic JSON definition populates the <c>ProviderAssembly</c> property.
		/// </summary>
		[TestMethod]
		public void ParseJson_DynamicDefinition_ShouldPopulateProviderAssembly()
		{
			var json = NotableDateDefinitionParserTests.DynamicDateJson;
			var definition = NotableDateDefinitionParser.ParseJson(json).Single();

			Assert.AreEqual("Bodu.Globalization.Calendar", definition.ProviderAssembly);
		}

		/// <summary>
		/// Verifies that parsing a rule-based JSON definition populates the <c>DayOfWeek</c> property.
		/// </summary>
		[TestMethod]
		public void ParseJson_RuleBasedDefinition_ShouldPopulateDayOfWeek()
		{
			var json = NotableDateDefinitionParserTests.RuleBasedDateJson;
			var definition = NotableDateDefinitionParser.ParseJson(json).Single();

			Assert.AreEqual(DayOfWeek.Monday, definition.DayOfWeek);
		}

		/// <summary>
		/// Verifies that parsing a rule-based JSON definition populates the <c>WeekOrdinal</c> property.
		/// </summary>
		[TestMethod]
		public void ParseJson_RuleBasedDefinition_ShouldPopulateWeekOrdinal()
		{
			var json = NotableDateDefinitionParserTests.RuleBasedDateJson;
			var definition = NotableDateDefinitionParser.ParseJson(json).Single();

			Assert.AreEqual(WeekOfMonthOrdinal.Second, definition.WeekOrdinal);
		}

		/// <summary>
		/// Verifies that parsing an offset-from JSON definition populates the <c>BaseNotableDateName</c> property.
		/// </summary>
		[TestMethod]
		public void ParseJson_OffsetFromDefinition_ShouldPopulateBaseNotableDateName()
		{
			var json = NotableDateDefinitionParserTests.OffsetFromDateJson;
			var definition = NotableDateDefinitionParser.ParseJson(json).Single();

			Assert.AreEqual("Easter Sunday", definition.BaseNotableDateName);
		}

		/// <summary>
		/// Verifies that parsing an offset-from JSON definition populates the <c>OffsetDays</c> property.
		/// </summary>
		[TestMethod]
		public void ParseJson_OffsetFromDefinition_ShouldPopulateOffsetDays()
		{
			var json = NotableDateDefinitionParserTests.OffsetFromDateJson;
			var definition = NotableDateDefinitionParser.ParseJson(json).Single();

			Assert.AreEqual(-2, definition.OffsetDays);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date JSON definition sets the <c>DefinitionType</c> to <see cref="NotableDateDefinitionType.Fixed" />.
		/// </summary>
		[TestMethod]
		public void ParseJson_FixedDefinition_ShouldPopulateDefinitionType()
		{
			var json = NotableDateDefinitionParserTests.FixedDateJson;
			var definition = NotableDateDefinitionParser.ParseJson(json).Single();

			Assert.AreEqual(NotableDateDefinitionType.Fixed, definition.DefinitionType);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date JSON definition populates the <c>NotableDateKind</c> property.
		/// </summary>
		[TestMethod]
		public void ParseJson_FixedDefinition_ShouldPopulateNotableDateKind()
		{
			var json = NotableDateDefinitionParserTests.FixedDateJson;
			var definition = NotableDateDefinitionParser.ParseJson(json).Single();

			Assert.AreEqual(NotableDateKind.Holiday, definition.NotableDateKind);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date JSON definition populates the <c>NonWorking</c> flag.
		/// </summary>
		[TestMethod]
		public void ParseJson_FixedDefinition_ShouldPopulateNonWorking()
		{
			var json = NotableDateDefinitionParserTests.FixedDateJson;
			var definition = NotableDateDefinitionParser.ParseJson(json).Single();

			Assert.IsTrue(definition.NonWorking ?? false);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date JSON definition populates both the <c>Day</c> and <c>Month</c> properties.
		/// </summary>
		[TestMethod]
		public void ParseJson_FixedDefinition_ShouldPopulateDayAndMonth()
		{
			var json = NotableDateDefinitionParserTests.FixedDateJson;
			var definition = NotableDateDefinitionParser.ParseJson(json).Single();

			Assert.AreEqual(1, definition.Day);
			Assert.AreEqual(1, definition.Month);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date JSON definition populates the <c>CalendarType</c> property with the expected calendar type name.
		/// </summary>
		[TestMethod]
		public void ParseJson_FixedDefinition_ShouldPopulateCalendarType()
		{
			var json = NotableDateDefinitionParserTests.FixedDateJson;
			var definition = NotableDateDefinitionParser.ParseJson(json).Single();

			Assert.AreEqual("System.Globalization.GregorianCalendar", definition.CalendarType);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date JSON definition populates the <c>Region</c> property.
		/// </summary>
		[TestMethod]
		public void ParseJson_FixedDefinition_ShouldPopulateRegion()
		{
			var json = NotableDateDefinitionParserTests.FixedDateJson;
			var definition = NotableDateDefinitionParser.ParseJson(json).Single();

			Assert.AreEqual("NSW", definition.Region);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date JSON definition populates the <c>OccurrenceYears</c> property.
		/// </summary>
		[TestMethod]
		public void ParseJson_FixedDefinition_ShouldPopulateOccurrenceYears()
		{
			var json = NotableDateDefinitionParserTests.FixedDateJson;
			var definition = NotableDateDefinitionParser.ParseJson(json).Single();

			Assert.AreEqual(4, definition.OccurrenceYears);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date JSON definition populates the <c>Comment</c> property.
		/// </summary>
		[TestMethod]
		public void ParseJson_FixedDefinition_ShouldPopulateComment()
		{
			var json = NotableDateDefinitionParserTests.FixedDateJson;
			var definition = NotableDateDefinitionParser.ParseJson(json).Single();

			Assert.AreEqual("Test comment", definition.Comment);
		}

		/// <summary>
		/// Verifies that parsing a fixed-date JSON definition correctly parses the embedded adjustment rules collection.
		/// </summary>
		[TestMethod]
		public void ParseJson_FixedDefinition_ShouldParseAdjustmentRules()
		{
			var json = NotableDateDefinitionParserTests.FixedDateJson;
			var definition = NotableDateDefinitionParser.ParseJson(json).Single();

			Assert.AreEqual(1, definition.AdjustmentRules.Count());
			Assert.AreEqual(NotableDateAdjustmentRuleType.IfWeekend, definition.AdjustmentRules[0].AdjustmentRule);
			Assert.AreEqual(NotableDateAdjustmentActionType.MoveToNextWeekday, definition.AdjustmentRules[0].Action);
		}
	}
}
