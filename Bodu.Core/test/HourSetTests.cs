// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HourSetTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Contracts;
using Bodu.Test.Kat;

namespace Bodu;

/// <summary>
/// Runs the calendar value set contract against <see cref="HourSet" />, whose domain is 0 to 23.
/// </summary>
[TestClass]
public sealed class HourSetTests
    : CalendarValueSetContractTests<HourSet>
{
    /// <inheritdoc />
    protected override int Minimum => 0;

    /// <inheritdoc />
    protected override int Maximum => 23;

    /// <inheritdoc />
    protected override string ConstructorParameterName => "hours";

    /// <inheritdoc />
    protected override string ElementParameterName => "hour";

    /// <inheritdoc />
    protected override IReadOnlyList<ValidKat<string, HourSet>> CanonicalTextCases { get; } =
    [
        new("midnight and noon", "0,12", new HourSet(0, 12)),
        new("business hours", "9-17", new HourSet(9, 10, 11, 12, 13, 14, 15, 16, 17)),
        new("overnight", "0-5,22-23", new HourSet(0, 1, 2, 3, 4, 5, 22, 23)),
        new("every sixth hour", "0,6,12,18", new HourSet(0, 6, 12, 18)),
    ];

    /// <inheritdoc />
    protected override HourSet Create(params int[]? values) =>
        new(values);

    /// <inheritdoc />
    protected override HourSet Parse(string s) =>
        HourSet.Parse(s);

    /// <inheritdoc />
    protected override bool TryParse(string? s, out HourSet result) =>
        HourSet.TryParse(s, out result);
}
