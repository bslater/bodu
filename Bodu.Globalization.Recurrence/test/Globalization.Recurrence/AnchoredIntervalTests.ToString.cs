// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AnchoredIntervalTests.ToString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class AnchoredIntervalTests
{
    /// <summary>
    /// Gets the canonical-rendering rows: an interval and the exact duration text it must render as.
    /// </summary>
    /// <value>The canonical-rendering rows.</value>
    public static IEnumerable<object[]> CanonicalKats
    {
        get
        {
            var rows = new List<ValidKat<TimeSpan, string>>
            {
                new("four hours", TimeSpan.FromHours(4), "PT4H"),
                new("one day", TimeSpan.FromDays(1), "P1D"),
                new("day, hours, and minutes", new TimeSpan(1, 2, 30, 0), "P1DT2H30M"),
                new("seven days renders as one week", TimeSpan.FromDays(7), "P1W"),
                new("fourteen days renders as two weeks", TimeSpan.FromDays(14), "P2W"),
                new("ninety seconds normalizes", TimeSpan.FromSeconds(90), "PT1M30S"),
                new("thirty-six hours normalizes", TimeSpan.FromHours(36), "P1DT12H"),
                new("forty-five seconds", TimeSpan.FromSeconds(45), "PT45S"),
                new("full date-time form", new TimeSpan(3, 4, 5, 6), "P3DT4H5M6S"),
            };

            foreach (ValidKat<TimeSpan, string> row in rows)
            {
                yield return [row];
            }
        }
    }

    /// <summary>
    /// Verifies that each interval renders as its exact canonical duration text.
    /// </summary>
    /// <param name="kat">The canonical-rendering row under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(CanonicalKats),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ToString_WhenCanonical_ShouldRenderExpectedText(ValidKat<TimeSpan, string> kat)
    {
        var interval = new AnchoredInterval(kat.Input);

        Assert.AreEqual(kat.Expected, interval.ToString());
    }

    /// <summary>
    /// Verifies that the canonical text re-parses to an equal interval.
    /// </summary>
    /// <param name="kat">The canonical-rendering row under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(CanonicalKats),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ToString_WhenReparsed_ShouldRoundTrip(ValidKat<TimeSpan, string> kat)
    {
        var interval = new AnchoredInterval(kat.Input);

        var reparsed = AnchoredInterval.Parse(interval.ToString());

        Assert.AreEqual(interval, reparsed);
    }

    /// <summary>
    /// Verifies that the general format specifier and the parameterless overload produce the same text.
    /// </summary>
    [TestMethod]
    public void ToString_WhenGeneralSpecifier_ShouldMatchDefault()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));

        Assert.AreEqual(interval.ToString(), interval.ToString("G", null));
        Assert.AreEqual(interval.ToString(), interval.ToString(null, null));
    }

    /// <summary>
    /// Verifies that an unsupported format specifier throws <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatSpecifierUnsupported_ShouldThrowFormatException()
    {
        var interval = new AnchoredInterval(TimeSpan.FromHours(4));

        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = interval.ToString("X", null);
        });

        Assert.IsTrue(ex.Message.Contains("'X'", StringComparison.Ordinal));
    }
}
