// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleParserTests.TestData.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Globalization.Calendar;

public partial class NotableDateRuleParserTests
{
	public const string FixedRuleXml = @"
		<NotableDates xmlns=""urn:bodu:globalization:calendar"">
			<NotableDate name=""Fixed Rule Test"">
				<Rule category=""Holiday""
				      firstYear=""2000""
				      lastYear=""2100""
				      occurrenceYears=""4""
				      durationDays=""1""
				      priority=""5""
				      nonWorking=""true""
				      territory=""AU-NSW""
				      calendarType=""System.Globalization.GregorianCalendar""
				      comment=""Fixed rule comment."">
					<Fixed month=""January"" day=""1"" />
					<Tag>Public</Tag>
					<Tag>Civic</Tag>
					<Adjustment when=""IfWeekend"" action=""MoveToNextWeekday"" priority=""10"" />
				</Rule>
			</NotableDate>
		</NotableDates>";

	public const string DayOfWeekInMonthRuleXml = @"
		<NotableDates xmlns=""urn:bodu:globalization:calendar"">
			<NotableDate name=""Mother's Day Test"">
				<Rule category=""Observance"">
					<DayOfWeekInMonth month=""May"" dayOfWeek=""Sunday"" weekOrdinal=""Second"" />
				</Rule>
			</NotableDate>
		</NotableDates>";

	public const string CalculatorRuleXml = @"
		<NotableDates xmlns=""urn:bodu:globalization:calendar"">
			<NotableDate name=""Easter Sunday Test"">
				<Rule category=""Observance"" firstYear=""1583"">
					<Calculator key=""easter-sunday"" />
				</Rule>
			</NotableDate>
		</NotableDates>";

	public const string OffsetFromAnchorRuleXml = @"
		<NotableDates xmlns=""urn:bodu:globalization:calendar"">
			<NotableDate name=""Good Friday Test"">
				<Rule category=""Holiday"">
					<OffsetFromAnchor name=""Easter Sunday"" offset=""-2"" />
				</Rule>
			</NotableDate>
		</NotableDates>";

	public const string MultiRuleXml = @"
		<NotableDates xmlns=""urn:bodu:globalization:calendar"">
			<NotableDate name=""New Year's Day"">
				<Rule category=""Holiday"" nonWorking=""true"">
					<Fixed month=""January"" day=""1"" />
					<Adjustment when=""IfWeekend"" action=""MoveToNextWeekday"" priority=""1"" />
				</Rule>
			</NotableDate>
			<NotableDate name=""Easter Sunday"">
				<Rule category=""Observance"" firstYear=""1583"">
					<Calculator key=""easter-sunday"" />
				</Rule>
			</NotableDate>
			<NotableDate name=""Good Friday"">
				<Rule category=""Holiday"" firstYear=""1583"" nonWorking=""true"">
					<OffsetFromAnchor name=""Easter Sunday"" offset=""-2"" />
				</Rule>
			</NotableDate>
			<NotableDate name=""Anzac Day"">
				<Rule category=""Remembrance"" territory=""AU,NZ"" nonWorking=""true"">
					<Fixed month=""April"" day=""25"" />
				</Rule>
			</NotableDate>
		</NotableDates>";

	public const string CherryPickXml = @"
		<NotableDates xmlns=""urn:bodu:globalization:calendar"">
			<UseFrom resource=""Bodu.Globalization.Calendar.Resources.Common.xml"">
				<Use name=""New Year's Day"" territory=""US"" />
				<Use name=""Halloween"" />
			</UseFrom>
			<UseFrom resource=""Bodu.Globalization.Calendar.Resources.Christian.xml"">
				<Use name=""Christmas Day"" as=""Christmas"" territory=""US"" nonWorking=""true"" priority=""5"" />
			</UseFrom>
			<NotableDate name=""Independence Day"">
				<Rule category=""Holiday"" territory=""US"" nonWorking=""true"">
					<Fixed month=""July"" day=""4"" />
				</Rule>
			</NotableDate>
		</NotableDates>";

	public const string UseAllXml = @"
		<NotableDates xmlns=""urn:bodu:globalization:calendar"">
			<UseFrom resource=""Bodu.Globalization.Calendar.Resources.Common.xml"">
				<UseAll />
			</UseFrom>
		</NotableDates>";
}
