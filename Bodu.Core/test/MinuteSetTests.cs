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
    protected override MinuteSet Empty => MinuteSet.Empty;

    /// <inheritdoc />
    protected override MinuteSet All => MinuteSet.All;

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
    protected override MinuteSet FromUInt64(ulong bits) =>
        MinuteSet.FromUInt64(bits);

    /// <inheritdoc />
    protected override ulong ToUInt64(MinuteSet set) =>
        set.ToUInt64();

    /// <inheritdoc />
    protected override int Count(MinuteSet set) =>
        set.Count;

    /// <inheritdoc />
    protected override bool Contains(MinuteSet set, int value) =>
        set.Contains(value);

    /// <inheritdoc />
    protected override MinuteSet With(MinuteSet set, int value) =>
        set.With(value);

    /// <inheritdoc />
    protected override MinuteSet Without(MinuteSet set, int value) =>
        set.Without(value);

    /// <inheritdoc />
    protected override MinuteSet Parse(string s) =>
        MinuteSet.Parse(s);

    /// <inheritdoc />
    protected override bool TryParse(string? s, out MinuteSet result) =>
        MinuteSet.TryParse(s, out result);

    /// <inheritdoc />
    protected override string Format(MinuteSet set, string? format) =>
        set.ToString(format);

    /// <inheritdoc />
    protected override MinuteSet ParseExact(string s, string format) =>
        MinuteSet.ParseExact(s, format);

    /// <inheritdoc />
    protected override bool TryParseExact(string? s, string? format, out MinuteSet result) =>
        MinuteSet.TryParseExact(s, format, out result);
}
