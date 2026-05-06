// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleJsonParserTests.TestData.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public partial class NotableDateRuleJsonParserTests
{
	public const string FixedRuleJson = @"
		{
			""notableDates"": [
				{
					""name"": ""Fixed Rule Test"",
					""rules"": [
						{
							""name"": ""Fixed Rule Test Rule"",
							""category"": ""Holiday"",
							""firstYear"": 2000,
							""lastYear"": 2100,
							""occurrenceYears"": 4,
							""durationDays"": 1,
							""priority"": 5,
							""nonWorking"": true,
							""territory"": ""AU-NSW"",
							""calendarType"": ""System.Globalization.GregorianCalendar"",
							""comment"": ""Fixed rule comment."",
							""fixed"": { ""month"": ""January"", ""day"": 1 },
							""tags"": [""Public"", ""Civic""],
							""adjustments"": [
								{ ""key"": ""weekend-roll"", ""when"": ""IfWeekend"", ""action"": ""MoveToNextWeekday"", ""priority"": 10 }
							]
						}
					]
				}
			]
		}";

	public const string DayOfWeekInMonthRuleJson = @"
		{
			""notableDates"": [
				{
					""name"": ""Mother's Day Test"",
					""rules"": [
						{
							""name"": ""Mother's Day Observance Rule"",
							""category"": ""Observance"",
							""dayOfWeekInMonth"": { ""month"": ""May"", ""dayOfWeek"": ""Sunday"", ""weekOrdinal"": ""Second"" }
						}
					]
				}
			]
		}";

	public const string AlgorithmRuleJson = @"
		{
			""notableDates"": [
				{
					""name"": ""Easter Sunday Test"",
					""rules"": [
						{
							""name"": ""Easter Sunday Algorithm Rule"",
							""category"": ""Observance"",
							""firstYear"": 1583,
							""algorithm"": { ""key"": ""easter-sunday"" }
						}
					]
				}
			]
		}";

	public const string OffsetFromAnchorRuleJson = @"
		{
			""notableDates"": [
				{
					""name"": ""Good Friday Test"",
					""rules"": [
						{
							""name"": ""Good Friday Offset Rule"",
							""category"": ""Holiday"",
							""offsetFromAnchor"": { ""name"": ""Easter Sunday"", ""offset"": -2 }
						}
					]
				}
			]
		}";

	public const string MultiRuleJson = @"
		{
			""notableDates"": [
				{
					""name"": ""New Year's Day"",
					""rules"": [
						{
							""name"": ""New Year's Day Rule"",
							""category"": ""Holiday"",
							""nonWorking"": true,
							""fixed"": { ""month"": ""January"", ""day"": 1 },
							""adjustments"": [
								{ ""key"": ""weekend-roll"", ""when"": ""IfWeekend"", ""action"": ""MoveToNextWeekday"", ""priority"": 1 }
							]
						}
					]
				},
				{
					""name"": ""Easter Sunday"",
					""rules"": [
						{ ""name"": ""Easter Sunday Rule"", ""category"": ""Observance"", ""firstYear"": 1583, ""algorithm"": { ""key"": ""easter-sunday"" } }
					]
				},
				{
					""name"": ""Good Friday"",
					""rules"": [
						{
							""name"": ""Good Friday Rule"",
							""category"": ""Holiday"",
							""firstYear"": 1583,
							""nonWorking"": true,
							""offsetFromAnchor"": { ""name"": ""Easter Sunday"", ""offset"": -2 }
						}
					]
				},
				{
					""name"": ""Anzac Day"",
					""rules"": [
						{
							""name"": ""Anzac Day Rule"",
							""category"": ""Remembrance"",
							""territory"": ""AU,NZ"",
							""nonWorking"": true,
							""fixed"": { ""month"": ""April"", ""day"": 25 }
						}
					]
				}
			]
		}";

	public const string CherryPickJson = @"
		{
			""useFrom"": [
				{
					""resource"": ""Bodu.Globalization.Calendar.Resources.Common.xml"",
					""uses"": [
						{ ""name"": ""New Year's Day"", ""territory"": ""US"" },
						{ ""name"": ""Halloween"" }
					]
				},
				{
					""resource"": ""Bodu.Globalization.Calendar.Resources.Christian.xml"",
					""uses"": [
						{ ""name"": ""Christmas Day"", ""as"": ""Christmas"", ""territory"": ""US"", ""nonWorking"": true, ""priority"": 5 }
					]
				}
			],
			""notableDates"": [
				{
					""name"": ""Independence Day"",
					""rules"": [
						{ ""name"": ""Independence Day Rule"", ""category"": ""Holiday"", ""territory"": ""US"", ""nonWorking"": true, ""fixed"": { ""month"": ""July"", ""day"": 4 } }
					]
				}
			]
		}";

	public const string UseAllJson = @"
		{
			""useFrom"": [
				{ ""resource"": ""Bodu.Globalization.Calendar.Resources.Common.xml"", ""useAll"": true }
			]
		}";

	public const string ReligiousRuleJson = @"
		{
			""notableDates"": [
				{
					""name"": ""Eid al-Fitr"",
					""rules"": [
						{ ""name"": ""Eid al-Fitr Rule"", ""category"": ""Religious"", ""algorithm"": { ""key"": ""islamic-eid-al-fitr"" } }
					]
				}
			]
		}";

	public const string CivicRuleJson = @"
		{
			""notableDates"": [
				{
					""name"": ""Constitution Day"",
					""rules"": [
						{ ""name"": ""Constitution Day Rule"", ""category"": ""Civic"", ""fixed"": { ""month"": ""July"", ""day"": 17 } }
					]
				}
			]
		}";
}
