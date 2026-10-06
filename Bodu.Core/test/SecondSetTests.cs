// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SecondSetTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Contracts;
using Bodu.Test.Kat;

namespace Bodu;

/// <summary>
/// Runs the calendar value set contract against <see cref="SecondSet" />, whose domain is 0 to 59.
/// </summary>
[TestClass]
public sealed class SecondSetTests
    : CalendarValueSetContractTests<SecondSet>
{
    /// <inheritdoc />
    protected override int Minimum => 0;

    /// <inheritdoc />
    protected override int Maximum => 59;

    /// <inheritdoc />
    protected override string ConstructorParameterName => "seconds";

    /// <inheritdoc />
    protected override string ElementParameterName => "second";

    /// <inheritdoc />
    protected override IReadOnlyList<ValidKat<string, SecondSet>> CanonicalTextCases { get; } =
    [
        new("every ten seconds", "0,10,20,30,40,50", new SecondSet(0, 10, 20, 30, 40, 50)),
        new("last five seconds", "55-59", new SecondSet(55, 56, 57, 58, 59)),
        new("first half of the minute", "0-29", new SecondSet(Enumerable.Range(0, 30).ToArray())),
        new("every fifteen seconds", "0,15,30,45", new SecondSet(0, 15, 30, 45)),
    ];

    /// <inheritdoc />
    protected override SecondSet Create(params int[]? values) =>
        new(values);

    /// <inheritdoc />
    protected override SecondSet Parse(string s) =>
        SecondSet.Parse(s);

    /// <inheritdoc />
    protected override bool TryParse(string? s, out SecondSet result) =>
        SecondSet.TryParse(s, out result);
}
