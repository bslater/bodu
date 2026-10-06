// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonthSetTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Contracts;
using Bodu.Test.Kat;

namespace Bodu;

/// <summary>
/// Runs the calendar value set contract against <see cref="MonthSet" />, whose domain is 1 to 12, and tests the letter
/// mask only months have.
/// </summary>
[TestClass]
public sealed partial class MonthSetTests
    : CalendarValueSetContractTests<MonthSet>
{
    /// <summary>
    /// Gets the letter-mask rows: a set and the mask the <c>J</c> format writes for it, January first, with <c>_</c>
    /// for a month not selected.
    /// </summary>
    private static IReadOnlyList<ValidKat<MonthSet, string>> LetterMaskCases { get; } =
    [
        new("no month", MonthSet.Empty, "____________"),
        new("every month", MonthSet.All, "JFMAMJJASOND"),
        new("January", new MonthSet(1), "J___________"),
        new("February", new MonthSet(2), "_F__________"),
        new("December", new MonthSet(12), "___________D"),
        new("first quarter and December", new MonthSet(1, 2, 3, 12), "JFM________D"),
        new("first month of each quarter", new MonthSet(1, 4, 7, 10), "J__A__J__O__"),
        new("northern summer", new MonthSet(6, 7, 8), "_____JJA____"),
        new("odd months", new MonthSet(1, 3, 5, 7, 9, 11), "J_M_M_J_S_N_"),
        new("even months", new MonthSet(2, 4, 6, 8, 10, 12), "_F_A_J_A_O_D"),
    ];

    /// <summary>
    /// Gets the letter-mask rows for <c>[DynamicData]</c>, one row per <see cref="LetterMaskCases" /> entry.
    /// </summary>
    public static IEnumerable<object[]> LetterMaskData =>
        LetterMaskCases.Select(kat => new object[] { kat });

    /// <inheritdoc />
    protected override int Minimum => 1;

    /// <inheritdoc />
    protected override int Maximum => 12;

    /// <inheritdoc />
    protected override string ConstructorParameterName => "months";

    /// <inheritdoc />
    protected override string ElementParameterName => "month";

    /// <inheritdoc />
    protected override IReadOnlyList<ValidKat<string, MonthSet>> CanonicalTextCases { get; } =
    [
        new("first month of each quarter", "1,4,7,10", new MonthSet(1, 4, 7, 10)),
        new("first quarter and December", "1-3,12", new MonthSet(1, 2, 3, 12)),
        new("northern summer", "6-8", new MonthSet(6, 7, 8)),
        new("odd months", "1,3,5,7,9,11", new MonthSet(1, 3, 5, 7, 9, 11)),
    ];

    /// <inheritdoc />
    protected override MonthSet Create(params int[]? values) =>
        new(values);

    /// <inheritdoc />
    protected override MonthSet Parse(string s) =>
        MonthSet.Parse(s);

    /// <inheritdoc />
    protected override bool TryParse(string? s, out MonthSet result) =>
        MonthSet.TryParse(s, out result);
}
