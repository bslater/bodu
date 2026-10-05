// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekSetKatTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu;

/// <summary>
/// Drives <see cref="ValidKat{TInput, TExpected}" /> rows against the <see cref="DayOfWeekSet" />
/// format and bitwise-operator surfaces. Bespoke per-operator coverage (commutativity, identity,
/// distinct error modes) remains in <see cref="DayOfWeekSetTests" /> partials - this class layers
/// KAT-driven coverage on top.
/// </summary>
[TestClass]
public sealed class DayOfWeekSetKatTests
{
    private static IReadOnlyList<ValidKat<DayOfWeekSet, string>> FormatKats { get; } =
        [
        new("empty",                      Input: DayOfWeekSet.Empty,    Expected: "_______"),
        new("all days",                   Input: DayOfWeekSet.All,  Expected: "SMTWTFS"),
        new("weekdays (Sunday-first)",    Input: DayOfWeekSet.Weekdays, Expected: "_MTWTF_"),
        new("weekend (Sunday-first)",     Input: DayOfWeekSet.Weekend,  Expected: "S_____S"),
    ];

    private static IReadOnlyList<ValidKat<(DayOfWeekSet Left, DayOfWeekSet Right), DayOfWeekSet>> AndKats { get; } =
        [
        new("any & Empty → Empty",            Input: (DayOfWeekSet.All,  DayOfWeekSet.Empty),    Expected: DayOfWeekSet.Empty),
        new("self & self → self",             Input: (DayOfWeekSet.Weekdays, DayOfWeekSet.Weekdays), Expected: DayOfWeekSet.Weekdays),
        new("Weekdays & Weekend → Empty",     Input: (DayOfWeekSet.Weekdays, DayOfWeekSet.Weekend),  Expected: DayOfWeekSet.Empty),
    ];

    private static IReadOnlyList<ValidKat<(DayOfWeekSet Left, DayOfWeekSet Right), DayOfWeekSet>> OrKats { get; } =
        [
        new("any | Empty → original",         Input: (DayOfWeekSet.Weekdays, DayOfWeekSet.Empty),    Expected: DayOfWeekSet.Weekdays),
        new("self | self → self",             Input: (DayOfWeekSet.Weekend,  DayOfWeekSet.Weekend),  Expected: DayOfWeekSet.Weekend),
        new("Weekdays | Weekend → AllDays",   Input: (DayOfWeekSet.Weekdays, DayOfWeekSet.Weekend),  Expected: DayOfWeekSet.All),
    ];

    private static IReadOnlyList<ValidKat<DayOfWeekSet, DayOfWeekSet>> ComplementKats { get; } =
        [
        new("~Empty → AllDays",     Input: DayOfWeekSet.Empty,    Expected: DayOfWeekSet.All),
        new("~AllDays → Empty",     Input: DayOfWeekSet.All,  Expected: DayOfWeekSet.Empty),
        new("~Weekdays → Weekend",  Input: DayOfWeekSet.Weekdays, Expected: DayOfWeekSet.Weekend),
        new("~Weekend → Weekdays",  Input: DayOfWeekSet.Weekend,  Expected: DayOfWeekSet.Weekdays),
    ];

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet" />.<c>ToString("S")</c> reproduces the canonical
    /// Sunday-first letter encoding for each <see cref="FormatKats" /> row.
    /// </summary>
    [TestMethod]
    public void ToString_WhenKatIsKnown_ShouldReturnExpectedEncoding()
    {
        foreach (ValidKat<DayOfWeekSet, string> kat in FormatKats)
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
        foreach (ValidKat<(DayOfWeekSet Left, DayOfWeekSet Right), DayOfWeekSet> kat in AndKats)
        {
            DayOfWeekSet actual = kat.Input.Left & kat.Input.Right;

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
        foreach (ValidKat<(DayOfWeekSet Left, DayOfWeekSet Right), DayOfWeekSet> kat in OrKats)
        {
            DayOfWeekSet actual = kat.Input.Left | kat.Input.Right;

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
        foreach (ValidKat<DayOfWeekSet, DayOfWeekSet> kat in ComplementKats)
        {
            DayOfWeekSet actual = ~kat.Input;

            Assert.AreEqual(
                kat.Expected,
                actual,
                $"Complement KAT '{kat.Name}': complement differs from expected.");
        }
    }
}
