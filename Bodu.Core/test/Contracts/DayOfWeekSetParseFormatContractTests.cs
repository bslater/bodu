// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekSetParseFormatContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.Contracts;

/// <summary>
/// Drives <see cref="ParseFormatContractTests{T}" /> against <see cref="DayOfWeekSet" />. The contract
/// base covers Parse(success), Parse(failure throws FormatException), TryParse(failure returns false),
/// and the parse → format → parse round trip. Bespoke DayOfWeekSet coverage (ParseExact, presets,
/// operators, bitmask equality) lives in <c>DayOfWeekSetTests.*</c>.
/// </summary>
[TestClass]
public sealed class DayOfWeekSetParseFormatContractTests
    : ParseFormatContractTests<DayOfWeekSet>
{
    /// <inheritdoc />
    protected override DayOfWeekSet Parse(string text, string? format = null, IFormatProvider? provider = null) =>
        DayOfWeekSet.Parse(text);

    /// <inheritdoc />
    protected override bool TryParse(string text, string? format, IFormatProvider? provider, out DayOfWeekSet value) =>
        DayOfWeekSet.TryParse(text, out value);

    /// <inheritdoc />
    protected override string Format(DayOfWeekSet value, string? format = null, IFormatProvider? provider = null) =>
        value.ToString(format ?? "S", provider);

    /// <inheritdoc />
    protected override IReadOnlyList<ValidKat<string, DayOfWeekSet>> ValidCases { get; } =
        [
        new("all days off (Sunday-first)",  Input: "_______",  Expected: DayOfWeekSet.Empty),
        new("all days on (Sunday-first)",   Input: "SMTWTFS",  Expected: DayOfWeekSet.All),
        new("weekdays (Sunday-first)",      Input: "_MTWTF_",  Expected: DayOfWeekSet.Weekdays),
        new("weekends (Sunday-first)",      Input: "S_____S",  Expected: DayOfWeekSet.Weekend),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<InvalidKat<string>> InvalidCases { get; } =
        [
        new("contains unrecognised character", Input: "SMTWTFX", ExceptionType: typeof(FormatException)),
        new("too short",                       Input: "SMTWTF",  ExceptionType: typeof(FormatException)),
        new("too long",                        Input: "SMTWTFSS", ExceptionType: typeof(FormatException)),
    ];
}
