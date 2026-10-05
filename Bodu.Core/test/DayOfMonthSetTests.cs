// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfMonthSetTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Contracts;
using Bodu.Test.Kat;

namespace Bodu;

/// <summary>
/// Runs the calendar value set contract against <see cref="DayOfMonthSet" />, whose domain is 1 to 31.
/// </summary>
[TestClass]
public sealed class DayOfMonthSetTests
    : CalendarValueSetContractTests<DayOfMonthSet>
{
    /// <inheritdoc />
    protected override int Minimum => 1;

    /// <inheritdoc />
    protected override int Maximum => 31;

    /// <inheritdoc />
    protected override DayOfMonthSet Empty => DayOfMonthSet.Empty;

    /// <inheritdoc />
    protected override DayOfMonthSet All => DayOfMonthSet.All;

    /// <inheritdoc />
    protected override string ConstructorParameterName => "days";

    /// <inheritdoc />
    protected override string ElementParameterName => "day";

    /// <inheritdoc />
    protected override IReadOnlyList<ValidKat<string, DayOfMonthSet>> CanonicalTextCases { get; } =
    [
        new("first and fifteenth", "1,15", new DayOfMonthSet(1, 15)),
        new("first week", "1-7", new DayOfMonthSet(1, 2, 3, 4, 5, 6, 7)),
        new("days only long months have", "29-31", new DayOfMonthSet(29, 30, 31)),
        new("every tenth day", "10,20,30", new DayOfMonthSet(10, 20, 30)),
    ];

    /// <inheritdoc />
    protected override DayOfMonthSet Create(params int[]? values) =>
        new(values);

    /// <inheritdoc />
    protected override DayOfMonthSet FromUInt64(ulong bits) =>
        DayOfMonthSet.FromUInt64(bits);

    /// <inheritdoc />
    protected override ulong ToUInt64(DayOfMonthSet set) =>
        set.ToUInt64();

    /// <inheritdoc />
    protected override int Count(DayOfMonthSet set) =>
        set.Count;

    /// <inheritdoc />
    protected override bool Contains(DayOfMonthSet set, int value) =>
        set.Contains(value);

    /// <inheritdoc />
    protected override DayOfMonthSet With(DayOfMonthSet set, int value) =>
        set.With(value);

    /// <inheritdoc />
    protected override DayOfMonthSet Without(DayOfMonthSet set, int value) =>
        set.Without(value);

    /// <inheritdoc />
    protected override DayOfMonthSet Parse(string s) =>
        DayOfMonthSet.Parse(s);

    /// <inheritdoc />
    protected override bool TryParse(string? s, out DayOfMonthSet result) =>
        DayOfMonthSet.TryParse(s, out result);
}
