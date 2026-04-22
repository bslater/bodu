// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDefinitionParserTests.TestData.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar
{
	public partial class NotableDateDefinitionParserTests
	{
		public static string ComplexNotableDatesXml => @"
			<NotableDates xmlns=""urn:bodu:SysGlobal:calendar"">
				<NotableDate name=""New Year's Day"">
					<Definition type=""Holiday"" nonWorking=""true"" country=""US"" comment=""Start of the new year"">
						<Fixed month=""January"" day=""1"" />
						<AdjustmentRule when=""IfWeekend"" action=""MoveToNextWeekday"" priority=""1"" />
					</Definition>
				</NotableDate>
				<NotableDate name=""Easter Sunday"">
					<Definition type=""Christian"">
						<Dynamic providerType=""Bodu.Globalization.Calendar.Calculators.EasterSundayNotableDateCalculator"" providerAssembly=""Bodu.Globalization.Calendar"" />
					</Definition>
				</NotableDate>
				<NotableDate name=""Second Monday in October"">
					<Definition type=""Holiday"" nonWorking=""true"" country=""AU"">
						<Rule month=""October"" dayOfWeek=""Monday"" weekOrdinal=""Second"" />
					</Definition>
				</NotableDate>
				<NotableDate name=""Good Friday"">
					<Definition type=""Christian"">
						<OffsetFrom name=""Easter Sunday"" offset=""-2"" />
					</Definition>
				</NotableDate>
			</NotableDates>";

		public static string ComplexNotableDatesJson => @"
			[
				{
					""Name"": ""New Year's Day"",
					""Type"": ""Fixed"",
					""Kind"": ""Holiday"",
					""FirstYear"": null,
					""LastYear"": null,
					""Calendar"": null,
					""TerritoryCode"": ""US"",
					""Region"": null,
					""NonWorking"": true,
					""Comment"": ""Start of the new year"",
					""Month"": 1,
					""Day"": 1,
					""AdjustmentRules"": [
						{
							""AdjustmentRule"": ""IfWeekend"",
							""Action"": ""MoveToNextWeekday"",
							""Priority"": 1
						}
					]
				},
				{
					""Name"": ""Easter Sunday"",
					""Type"": ""Dynamic"",
					""Kind"": ""Christian"",
					""FirstYear"": null,
					""LastYear"": null,
					""Calendar"": null,
					""TerritoryCode"": null,
					""Region"": null,
					""NonWorking"": null,
					""Comment"": null,
					""NotableDateCalculatorType"": ""Bodu.Globalization.Calendar"",
					""ProviderTypeName"": ""Bodu.Globalization.Calendar.Calculators.EasterSundayNotableDateCalculator""
				},
				{
					""Name"": ""Second Monday in October"",
					""Type"": ""Rule"",
					""Kind"": ""Holiday"",
					""FirstYear"": null,
					""LastYear"": null,
					""Calendar"": null,
					""TerritoryCode"": ""AU"",
					""Region"": null,
					""NonWorking"": true,
					""Comment"": null,
					""Month"": 10,
					""DayOfWeek"": ""Monday"",
					""WeekOrdinal"": ""Second""
				},
				{
					""Name"": ""Good Friday"",
					""Type"": ""OffsetFrom"",
					""Kind"": ""Christian"",
					""FirstYear"": null,
					""LastYear"": null,
					""Calendar"": null,
					""TerritoryCode"": null,
					""Region"": null,
					""NonWorking"": null,
					""Comment"": null,
					""BaseNotableDateName"": ""Easter Sunday"",
					""OffsetDays"": -2
				}
			]";

		public static string FixedDateXml => @"
			<NotableDates xmlns=""urn:bodu:SysGlobal:calendar"">
				<NotableDate name=""Fixed Date Test"">
					<Definition type=""Holiday"" firstYear=""2000"" lastYear=""2100"" nonWorking=""true"" country=""AU"" subRegion=""NSW"" calendarType=""System.Globalization.GregorianCalendar"" comment=""Fixed date comment."" occurrenceYears=""4"">
						<Fixed month=""January"" day=""1"" />
						<AdjustmentRule when=""IfWeekend"" action=""MoveToNextWeekday"" priority=""10"" />
					</Definition>
				</NotableDate>
			</NotableDates>";

		public static string FixedDateJson => @"
			[
			  {
				""Name"": ""Fixed Date Test"",
				""Type"": ""Fixed"",
				""Kind"": ""Holiday"",
				""FirstYear"": 2000,
				""LastYear"": 2100,
				""NonWorking"": true,
				""TerritoryCode"": ""AU"",
				""Region"": ""NSW"",
				""Comment"": ""Fixed date comment."",
				""Day"": 1,
				""Month"": 1,
				""AdjustmentRules"": [
				  {
					""AdjustmentRule"": ""IfWeekend"",
					""Action"": ""MoveToNextWeekday"",
					""Priority"": 10
				  }
				]
			  }
			]";

		public static string DynamicDateXml => @"
			<NotableDates xmlns=""urn:bodu:SysGlobal:calendar"">
				<NotableDate name=""Dynamic Date Test"">
					<Definition type=""Holiday"" firstYear=""2000"">
						<Dynamic providerType=""Bodu.Globalization.Calendar.Calculators.EasterSundayNotableDateCalculator"" providerAssembly=""Bodu.Globalization.Calendar"" />
					</Definition>
				</NotableDate>
			</NotableDates>";

		public static string DynamicDateJson => @"
			[
			  {
				""Name"": ""Dynamic Date Test"",
				""Type"": ""Dynamic"",
				""Kind"": ""Holiday"",
				""FirstYear"": 2000,
				""ProviderTypeName"": ""Bodu.Globalization.Calendar.Calculators.EasterSundayNotableDateCalculator"",
				""NotableDateCalculatorType"": ""Bodu.Globalization.Calendar""
			  }
			]";

		public static string RuleBasedDateXml => @"
			<NotableDates xmlns=""urn:bodu:SysGlobal:calendar"">
				<NotableDate name=""Rule Date Test"">
					<Definition type=""Holiday"">
						<Rule month=""October"" dayOfWeek=""Monday"" weekOrdinal=""Second"" />
					</Definition>
				</NotableDate>
			</NotableDates>";

		public static string RuleBasedDateJson => @"
			[
			  {
				""Name"": ""Rule Date Test"",
				""Type"": ""Rule"",
				""Kind"": ""Holiday"",
				""Month"": 10,
				""DayOfWeek"": ""Monday"",
				""WeekOrdinal"": ""Second""
			  }
			]";

		public static string OffsetFromDateXml => @"
			<NotableDates xmlns=""urn:bodu:SysGlobal:calendar"">
				<NotableDate name=""Offset Date Test"">
					<Definition type=""Holiday"">
						<OffsetFrom name=""Easter Sunday"" offset=""-2"" />
					</Definition>
				</NotableDate>
			</NotableDates>";

		public static string OffsetFromDateJson => @"
			[
			  {
				""Name"": ""Offset Date Test"",
				""Type"": ""OffsetFrom"",
				""Kind"": ""Holiday"",
				""BaseNotableDateName"": ""Easter Sunday"",
				""OffsetDays"": -2
			  }
			]";

		/// <summary>
		/// Returns the expected parsed NotableDateDefinition list corresponding to ComplexNotableDatesXml.
		/// </summary>
		public static List<NotableDateDefinition> GetComplexExpectedDefinitions()
		{
			return new List<NotableDateDefinition>
			{
				new NotableDateDefinition
				{
					Name = "New Year's Day",
					DefinitionType = NotableDateDefinitionType.Fixed,
					NotableDateKind = NotableDateKind.Holiday,
					NonWorking=true,
					Country="US",
					Comment="Start of the new year",
					AdjustmentRules = ImmutableArray.Create<NotableDateAdjustmentRule>(
						new NotableDateAdjustmentRule
						{
							AdjustmentRule= NotableDateAdjustmentRuleType.IfWeekend,
							Action= NotableDateAdjustmentActionType.MoveToNextWeekday,
							Priority=1
						}
					),
				},
				new NotableDateDefinition
				{
					Name = "Easter Sunday",
					DefinitionType = NotableDateDefinitionType.Dynamic,
					NotableDateKind = NotableDateKind.Christian,
					ProviderTypeName = "Bodu.Globalization.Calendar.Calculators.EasterSundayNotableDateCalculator",
					NotableDateCalculatorType = "Bodu.Globalization.Calendar",
				},
				new NotableDateDefinition
				{
					Name="Second Monday in October",
					DefinitionType=NotableDateDefinitionType.Rule,
					NotableDateKind=NotableDateKind.Holiday,
					Country="AU",
					NonWorking=true,
					Month=10,
					DayOfWeek=DayOfWeek.Monday,
					WeekOrdinal=WeekOfMonthOrdinal.Second,
				},
				new NotableDateDefinition
				{
					Name="Good Friday",
					DefinitionType=NotableDateDefinitionType.OffsetFrom,
					NotableDateKind=NotableDateKind.Christian,
					BaseNotableDateName="Easter Sunday",
					OffsetDays=-2,
				}
			}
			.ToList();
		}
	}
}