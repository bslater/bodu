// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AnchoredIntervalTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Contains unit tests for the <see cref="AnchoredInterval" /> type.
/// </summary>
[TestClass]
public sealed partial class AnchoredIntervalTests
{
    /// <summary>
    /// Gets the valid RFC 5545 §3.3.6 duration rows and the interval each parses to.
    /// </summary>
    /// <value>The valid duration rows.</value>
    public static IEnumerable<object[]> ParseValidKats
    {
        get
        {
            var rows = new List<ValidKat<string, TimeSpan>>
            {
                new("four hours", "PT4H", TimeSpan.FromHours(4)),
                new("one day", "P1D", TimeSpan.FromDays(1)),
                new("day, hours, and minutes", "P1DT2H30M", new TimeSpan(1, 2, 30, 0)),
                new("two weeks", "P2W", TimeSpan.FromDays(14)),
                new("hours and minutes", "PT1H30M", new TimeSpan(1, 30, 0)),
                new("non-normalized seconds", "PT90S", TimeSpan.FromSeconds(90)),
                new("non-normalized hours", "PT36H", TimeSpan.FromHours(36)),
                new("zero component with positive total", "P0DT1H", TimeSpan.FromHours(1)),
                new("seven days as days", "P7D", TimeSpan.FromDays(7)),
                new("lowercase designators", "pt4h", TimeSpan.FromHours(4)),
                new("surrounding white space", " PT4H ", TimeSpan.FromHours(4)),
                new("full date-time form", "P3DT4H5M6S", new TimeSpan(3, 4, 5, 6)),

                // RFC 5545 dur-value permits a leading sign; the positive form must be accepted, not rejected
                // alongside the negative one (luxon, sosodev, NodaTime and XmlConvert all reject it).
                new("explicit leading plus", "+PT4H", TimeSpan.FromHours(4)),
                new("RFC 3.3.6 example P15DT5H0M20S", "P15DT5H0M20S", new TimeSpan(15, 5, 0, 20)),
                new("RFC 3.3.6 example P7W", "P7W", TimeSpan.FromDays(49)),
            };

            foreach (ValidKat<string, TimeSpan> row in rows)
            {
                yield return [row];
            }
        }
    }

    /// <summary>
    /// Gets the malformed duration rows, each carrying the defect fragment its failure message must name.
    /// </summary>
    /// <value>The malformed duration rows.</value>
    public static IEnumerable<object[]> ParseInvalidKats
    {
        get
        {
            var rows = new List<InvalidKat<string>>
            {
                new("empty text", string.Empty, typeof(FormatException), MessageContains: "empty"),
                new("white space only", "   ", typeof(FormatException), MessageContains: "empty"),
                new("missing P designator", "4H", typeof(FormatException), MessageContains: "begin with"),
                new("negative duration", "-PT4H", typeof(FormatException), MessageContains: "greater than zero"),
                new("bare designator", "P", typeof(FormatException), MessageContains: "at least one component"),
                new("bare time designator", "PT", typeof(FormatException), MessageContains: "at least one component"),
                new("zero days", "P0D", typeof(FormatException), MessageContains: "greater than zero"),
                new("zero seconds", "PT0S", typeof(FormatException), MessageContains: "greater than zero"),
                new("hours without time designator", "P4H", typeof(FormatException), MessageContains: "'T' designator"),
                new("trailing digits without unit", "PT4H30", typeof(FormatException), MessageContains: "not valid"),
                new("unknown unit", "PT4X", typeof(FormatException), MessageContains: "not valid"),
                new("unit without digits", "PTH", typeof(FormatException), MessageContains: "not valid"),
                new("weeks combined with days", "P1W2D", typeof(FormatException), MessageContains: "cannot be combined"),
                new("weeks combined with time", "P1WT1H", typeof(FormatException), MessageContains: "cannot be combined"),
                new("minutes before hours", "PT1M2H", typeof(FormatException), MessageContains: "repeated or out of order"),
                new("repeated days", "P1D2D", typeof(FormatException), MessageContains: "repeated or out of order"),
                new("repeated time designator", "PTT1H", typeof(FormatException), MessageContains: "repeated or out of order"),
                new("days after time designator", "PT1D", typeof(FormatException), MessageContains: "repeated or out of order"),
                new("component digits overflow", "P99999999999999999999D", typeof(FormatException), MessageContains: "too large"),
                new("total exceeds the representable range", "P10675200D", typeof(FormatException), MessageContains: "too large"),

                // dur-time = "T" (dur-hour / dur-minute / dur-second) — the designator requires a component after it.
                // Accepted by luxon, NodaTime, rickb777, sosodev, python icalendar and isodate; rejected by
                // java.time, Temporal and XmlConvert.
                new("trailing time designator", "P1DT", typeof(FormatException), MessageContains: "at least one component"),
                new("bare trailing designator after weeks", "P1WT", typeof(FormatException), MessageContains: "at least one component"),

                // RFC 5545 excludes the year and month designators because they are not fixed-length. "P1M" is the
                // headline interop hazard: ical.js reads it as one minute, everything else as one month.
                new("month designator", "P1M", typeof(FormatException), MessageContains: "'T' designator"),
                new("year designator", "P1Y", typeof(FormatException), MessageContains: "not valid"),
                new("month designator seen in the wild", "P90M", typeof(FormatException), MessageContains: "'T' designator"),

                // A sign is legal only in the leading position.
                new("sign before a component", "PT-4H", typeof(FormatException), MessageContains: "not valid"),
                new("sign between components", "PT4H-30M", typeof(FormatException), MessageContains: "not valid"),
                new("double sign", "-PT-4H", typeof(FormatException), MessageContains: "greater than zero"),

                // The P designator is mandatory even when a time designator is present.
                new("time designator without P", "T4H", typeof(FormatException), MessageContains: "begin with"),

                // Overflow must be a deterministic parse error, never a silently wrapped or negative duration.
                new("seconds overflow to negative", "PT9223372036854775807S", typeof(FormatException), MessageContains: "too large"),
                new("hours overflow to negative", "PT2147483648H", typeof(FormatException), MessageContains: "too large"),
                new("weeks overflow", "P2147483647W", typeof(FormatException), MessageContains: "too large"),
            };

            foreach (InvalidKat<string> row in rows)
            {
                yield return [row];
            }
        }
    }
}
