// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternParseFormatContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.Contracts;

/// <summary>
/// Drives <see cref="ParseFormatContractTests{T}" /> against <see cref="WeekPattern" />. The contract
/// base covers Parse(success), Parse(failure throws FormatException), TryParse(failure returns false),
/// and the parse → format → parse round trip. Bespoke WeekPattern coverage (ParseExact, presets,
/// operators, bitmask equality) lives in <c>WeekPatternTests.*</c>.
/// </summary>
[TestClass]
public sealed class WeekPatternParseFormatContractTests : ParseFormatContractTests<WeekPattern>
{
    /// <inheritdoc />
    protected override WeekPattern Parse(string text, string? format = null, IFormatProvider? provider = null) =>
        WeekPattern.Parse(text);

    /// <inheritdoc />
    protected override bool TryParse(string text, string? format, IFormatProvider? provider, out WeekPattern value) =>
        WeekPattern.TryParse(text, out value);

    /// <inheritdoc />
    protected override string Format(WeekPattern value, string? format = null, IFormatProvider? provider = null) =>
        value.ToString(format ?? "S", provider);

    /// <inheritdoc />
    protected override IReadOnlyList<ValidKat<string, WeekPattern>> ValidCases { get; } =
        [
        new("all days off (Sunday-first)",  Input: "_______",  Expected: WeekPattern.Empty),
        new("all days on (Sunday-first)",   Input: "SMTWTFS",  Expected: WeekPattern.AllDays),
        new("weekdays (Sunday-first)",      Input: "_MTWTF_",  Expected: WeekPattern.Weekdays),
        new("weekends (Sunday-first)",      Input: "S_____S",  Expected: WeekPattern.Weekend),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<InvalidKat<string>> InvalidCases { get; } =
        [
        new("contains unrecognised character", Input: "SMTWTFX", ExceptionType: typeof(FormatException)),
        new("too short",                       Input: "SMTWTF",  ExceptionType: typeof(FormatException)),
        new("too long",                        Input: "SMTWTFSS", ExceptionType: typeof(FormatException)),
    ];
}
