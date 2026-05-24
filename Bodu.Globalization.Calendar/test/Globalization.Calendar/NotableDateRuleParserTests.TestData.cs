// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleParserTests.TestData.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Globalization.Calendar;

public partial class NotableDateRuleParserTests
{
    public const string FixedRuleXml = @"
		<NotableDates xmlns=""urn:bodu:globalization:calendar"">
			<NotableDate name=""Fixed Rule Test"">
				<Rule name=""Fixed Rule Test Rule""
				      category=""Holiday""
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
					<Adjustment key=""weekend-roll"" when=""IfWeekend"" action=""MoveToNextWeekday"" priority=""10"" />
				</Rule>
			</NotableDate>
		</NotableDates>";

    public const string DayOfWeekInMonthRuleXml = @"
		<NotableDates xmlns=""urn:bodu:globalization:calendar"">
			<NotableDate name=""Mother's Day Test"">
				<Rule name=""Mother's Day Observance Rule"" category=""Observance"">
					<DayOfWeekInMonth month=""May"" dayOfWeek=""Sunday"" weekOrdinal=""Second"" />
				</Rule>
			</NotableDate>
		</NotableDates>";

    public const string AlgorithmRuleXml = @"
		<NotableDates xmlns=""urn:bodu:globalization:calendar"">
			<NotableDate name=""Easter Sunday Test"">
				<Rule name=""Easter Sunday Algorithm Rule"" category=""Observance"" firstYear=""1583"">
					<Algorithm key=""easter-sunday"" />
				</Rule>
			</NotableDate>
		</NotableDates>";

    public const string OffsetFromAnchorRuleXml = @"
		<NotableDates xmlns=""urn:bodu:globalization:calendar"">
			<NotableDate name=""Good Friday Test"">
				<Rule name=""Good Friday Offset Rule"" category=""Holiday"">
					<OffsetFromAnchor name=""Easter Sunday"" offset=""-2"" />
				</Rule>
			</NotableDate>
		</NotableDates>";

    public const string MultiRuleXml = @"
		<NotableDates xmlns=""urn:bodu:globalization:calendar"">
			<NotableDate name=""New Year's Day"">
				<Rule name=""New Year's Day Rule"" category=""Holiday"" nonWorking=""true"">
					<Fixed month=""January"" day=""1"" />
					<Adjustment key=""weekend-roll"" when=""IfWeekend"" action=""MoveToNextWeekday"" priority=""1"" />
				</Rule>
			</NotableDate>
			<NotableDate name=""Easter Sunday"">
				<Rule name=""Easter Sunday Rule"" category=""Observance"" firstYear=""1583"">
					<Algorithm key=""easter-sunday"" />
				</Rule>
			</NotableDate>
			<NotableDate name=""Good Friday"">
				<Rule name=""Good Friday Rule"" category=""Holiday"" firstYear=""1583"" nonWorking=""true"">
					<OffsetFromAnchor name=""Easter Sunday"" offset=""-2"" />
				</Rule>
			</NotableDate>
			<NotableDate name=""Anzac Day"">
				<Rule name=""Anzac Day Rule"" category=""Remembrance"" territory=""AU,NZ"" nonWorking=""true"">
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
				<Rule name=""Independence Day Rule"" category=""Holiday"" territory=""US"" nonWorking=""true"">
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

    public const string ReligiousRuleXml = @"
		<NotableDates xmlns=""urn:bodu:globalization:calendar"">
			<NotableDate name=""Eid al-Fitr"">
				<Rule name=""Eid al-Fitr Rule"" category=""Religious"">
					<Algorithm key=""islamic-eid-al-fitr"" />
				</Rule>
			</NotableDate>
		</NotableDates>";

    public const string CivicRuleXml = @"
		<NotableDates xmlns=""urn:bodu:globalization:calendar"">
			<NotableDate name=""Constitution Day"">
				<Rule name=""Constitution Day Rule"" category=""Civic"">
					<Fixed month=""July"" day=""17"" />
				</Rule>
			</NotableDate>
		</NotableDates>";
}
