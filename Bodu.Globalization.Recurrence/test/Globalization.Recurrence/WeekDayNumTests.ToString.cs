// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekDayNumTests.ToString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class WeekDayNumTests
{
    /// <summary>
    /// Gets the token rows: an entry and the exact <c>BYDAY</c> token it must render as.
    /// </summary>
    /// <value>The token rows.</value>
    public static IEnumerable<object[]> TokenKats
    {
        get
        {
            var rows = new List<ValidKat<WeekDayNum, string>>
            {
                new("every Sunday", new WeekDayNum(0, DayOfWeek.Sunday), "SU"),
                new("every Monday", new WeekDayNum(0, DayOfWeek.Monday), "MO"),
                new("every Tuesday", new WeekDayNum(0, DayOfWeek.Tuesday), "TU"),
                new("every Wednesday", new WeekDayNum(0, DayOfWeek.Wednesday), "WE"),
                new("every Thursday", new WeekDayNum(0, DayOfWeek.Thursday), "TH"),
                new("every Friday", new WeekDayNum(0, DayOfWeek.Friday), "FR"),
                new("every Saturday", new WeekDayNum(0, DayOfWeek.Saturday), "SA"),
                new("first Monday", new WeekDayNum(1, DayOfWeek.Monday), "1MO"),
                new("third Thursday", new WeekDayNum(3, DayOfWeek.Thursday), "3TH"),
                new("fifty-third Sunday", new WeekDayNum(53, DayOfWeek.Sunday), "53SU"),
                new("last Friday", new WeekDayNum(-1, DayOfWeek.Friday), "-1FR"),
                new("second-to-last Tuesday", new WeekDayNum(-2, DayOfWeek.Tuesday), "-2TU"),
                new("fifty-third-to-last Saturday", new WeekDayNum(-53, DayOfWeek.Saturday), "-53SA"),
            };

            foreach (ValidKat<WeekDayNum, string> row in rows)
            {
                yield return [row];
            }
        }
    }

    /// <summary>
    /// Verifies that each entry renders as its exact <c>BYDAY</c> token: the ordinal when it is not zero, with no sign
    /// when it is positive, followed by the two-letter weekday.
    /// </summary>
    /// <param name="kat">The token row under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(TokenKats),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ToString_WhenDayIsDefined_ShouldRenderBydayToken(ValidKat<WeekDayNum, string> kat)
    {
        Assert.AreEqual(kat.Expected, kat.Input.ToString());
    }

    /// <summary>
    /// Verifies that every entry a rule can hold, each weekday at each ordinal from -53 to 53, renders as the token the
    /// rule's own text writes for it.
    /// </summary>
    [TestMethod]
    public void ToString_WhenRuleCarriesEntry_ShouldMatchRuleText()
    {
        for (var ordinal = -53; ordinal <= 53; ordinal++)
        {
            for (var day = DayOfWeek.Sunday; day <= DayOfWeek.Saturday; day++)
            {
                var entry = new WeekDayNum(ordinal, day);
                RecurrenceRule rule = new RecurrenceRuleBuilder(RecurrenceFrequency.Yearly)
                    .ByDay(entry)
                    .Build();

                Assert.AreEqual(rule.ToString(), "FREQ=YEARLY;BYDAY=" + entry.ToString(), $"ordinal {ordinal}, {day}");
            }
        }
    }

    /// <summary>
    /// Verifies that an entry whose day is not a defined <see cref="DayOfWeek" />, which no rule can hold, keeps the
    /// record's member text rather than being written as a token that names some other day.
    /// </summary>
    /// <param name="day">The undefined day value.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(7)]
    public void ToString_WhenDayIsUndefined_ShouldKeepRecordText(int day)
    {
        var entry = new WeekDayNum(1, (DayOfWeek)day);

        Assert.AreEqual($"WeekDayNum {{ Ordinal = 1, Day = {day}, IsEveryOccurrence = False }}", entry.ToString());
    }
}
