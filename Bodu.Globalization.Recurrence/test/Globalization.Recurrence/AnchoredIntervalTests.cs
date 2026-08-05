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
                new("signed duration", "+PT4H", typeof(FormatException), MessageContains: "greater than zero"),
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
            };

            foreach (InvalidKat<string> row in rows)
            {
                yield return [row];
            }
        }
    }
}
