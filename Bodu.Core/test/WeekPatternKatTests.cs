// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternKatTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu;

/// <summary>
/// Drives <see cref="ValidKat{TInput, TExpected}" /> rows against the <see cref="WeekPattern" />
/// format and bitwise-operator surfaces. Bespoke per-operator coverage (commutativity, identity,
/// distinct error modes) remains in <see cref="WeekPatternTests" /> partials — this class layers
/// KAT-driven coverage on top.
/// </summary>
[TestClass]
public sealed class WeekPatternKatTests
{
    private static IReadOnlyList<ValidKat<WeekPattern, string>> FormatKats { get; } =
        [
        new("empty",                      Input: WeekPattern.Empty,    Expected: "_______"),
        new("all days",                   Input: WeekPattern.AllDays,  Expected: "SMTWTFS"),
        new("weekdays (Sunday-first)",    Input: WeekPattern.Weekdays, Expected: "_MTWTF_"),
        new("weekend (Sunday-first)",     Input: WeekPattern.Weekend,  Expected: "S_____S"),
    ];

    private static IReadOnlyList<ValidKat<(WeekPattern Left, WeekPattern Right), WeekPattern>> AndKats { get; } =
        [
        new("any & Empty → Empty",            Input: (WeekPattern.AllDays,  WeekPattern.Empty),    Expected: WeekPattern.Empty),
        new("self & self → self",             Input: (WeekPattern.Weekdays, WeekPattern.Weekdays), Expected: WeekPattern.Weekdays),
        new("Weekdays & Weekend → Empty",     Input: (WeekPattern.Weekdays, WeekPattern.Weekend),  Expected: WeekPattern.Empty),
    ];

    private static IReadOnlyList<ValidKat<(WeekPattern Left, WeekPattern Right), WeekPattern>> OrKats { get; } =
        [
        new("any | Empty → original",         Input: (WeekPattern.Weekdays, WeekPattern.Empty),    Expected: WeekPattern.Weekdays),
        new("self | self → self",             Input: (WeekPattern.Weekend,  WeekPattern.Weekend),  Expected: WeekPattern.Weekend),
        new("Weekdays | Weekend → AllDays",   Input: (WeekPattern.Weekdays, WeekPattern.Weekend),  Expected: WeekPattern.AllDays),
    ];

    private static IReadOnlyList<ValidKat<WeekPattern, WeekPattern>> ComplementKats { get; } =
        [
        new("~Empty → AllDays",     Input: WeekPattern.Empty,    Expected: WeekPattern.AllDays),
        new("~AllDays → Empty",     Input: WeekPattern.AllDays,  Expected: WeekPattern.Empty),
        new("~Weekdays → Weekend",  Input: WeekPattern.Weekdays, Expected: WeekPattern.Weekend),
        new("~Weekend → Weekdays",  Input: WeekPattern.Weekend,  Expected: WeekPattern.Weekdays),
    ];

    /// <summary>
    /// Verifies that <see cref="WeekPattern" />.<c>ToString("S")</c> reproduces the canonical
    /// Sunday-first letter encoding for each <see cref="FormatKats" /> row.
    /// </summary>
    [TestMethod]
    public void ToString_WhenKatIsKnown_ShouldReturnExpectedEncoding()
    {
        foreach (ValidKat<WeekPattern, string> kat in FormatKats)
        {
            string actual = kat.Input.ToString("S", null);

            Assert.AreEqual(
                kat.Expected,
                actual,
                $"Format KAT '{kat.Name}': encoded value differs from expected.");
        }
    }

    /// <summary>
    /// Verifies that the bitwise <c>&amp;</c> operator reproduces the expected intersection for each
    /// <see cref="AndKats" /> row.
    /// </summary>
    [TestMethod]
    public void AndOperator_WhenKatIsKnown_ShouldReturnExpectedIntersection()
    {
        foreach (ValidKat<(WeekPattern Left, WeekPattern Right), WeekPattern> kat in AndKats)
        {
            WeekPattern actual = kat.Input.Left & kat.Input.Right;

            Assert.AreEqual(
                kat.Expected,
                actual,
                $"AND KAT '{kat.Name}': intersection differs from expected.");
        }
    }

    /// <summary>
    /// Verifies that the bitwise <c>|</c> operator reproduces the expected union for each
    /// <see cref="OrKats" /> row.
    /// </summary>
    [TestMethod]
    public void OrOperator_WhenKatIsKnown_ShouldReturnExpectedUnion()
    {
        foreach (ValidKat<(WeekPattern Left, WeekPattern Right), WeekPattern> kat in OrKats)
        {
            WeekPattern actual = kat.Input.Left | kat.Input.Right;

            Assert.AreEqual(
                kat.Expected,
                actual,
                $"OR KAT '{kat.Name}': union differs from expected.");
        }
    }

    /// <summary>
    /// Verifies that the <c>~</c> operator reproduces the expected complement for each
    /// <see cref="ComplementKats" /> row.
    /// </summary>
    [TestMethod]
    public void ComplementOperator_WhenKatIsKnown_ShouldReturnExpectedComplement()
    {
        foreach (ValidKat<WeekPattern, WeekPattern> kat in ComplementKats)
        {
            WeekPattern actual = ~kat.Input;

            Assert.AreEqual(
                kat.Expected,
                actual,
                $"Complement KAT '{kat.Name}': complement differs from expected.");
        }
    }
}
