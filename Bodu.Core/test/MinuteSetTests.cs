// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MinuteSetTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Contracts;
using Bodu.Test.Kat;

namespace Bodu;

/// <summary>
/// Runs the calendar value set contract against <see cref="MinuteSet" />, whose domain is 0 to 59.
/// </summary>
[TestClass]
public sealed class MinuteSetTests
    : CalendarValueSetContractTests<MinuteSet>
{
    /// <inheritdoc />
    protected override int Minimum => 0;

    /// <inheritdoc />
    protected override int Maximum => 59;

    /// <inheritdoc />
    protected override string ConstructorParameterName => "minutes";

    /// <inheritdoc />
    protected override string ElementParameterName => "minute";

    /// <inheritdoc />
    protected override IReadOnlyList<ValidKat<string, MinuteSet>> CanonicalTextCases { get; } =
    [
        new("quarter hours", "0,15,30,45", new MinuteSet(0, 15, 30, 45)),
        new("first five minutes", "0-4", new MinuteSet(0, 1, 2, 3, 4)),
        new("half past and the last minute", "30,59", new MinuteSet(30, 59)),
        new("runs either side of the hour", "0-2,57-59", new MinuteSet(0, 1, 2, 57, 58, 59)),
    ];

    /// <inheritdoc />
    protected override MinuteSet Create(params int[]? values) =>
        new(values);

    /// <inheritdoc />
    protected override MinuteSet Parse(string s) =>
        MinuteSet.Parse(s);

    /// <inheritdoc />
    protected override bool TryParse(string? s, out MinuteSet result) =>
        MinuteSet.TryParse(s, out result);
}
