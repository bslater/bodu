// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimalParseFormatContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;
using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.Numerics.Contracts;

/// <summary>
/// Drives <see cref="ParseFormatContractTests{T}" /> against <see cref="BigDecimal" />. The contract base covers
/// Parse(success), Parse(failure throws FormatException), TryParse(failure returns false), and the parse → format →
/// parse round trip. The overrides pin the invariant culture so the decimal separator is deterministic regardless of
/// the test host culture. Bespoke BigDecimal tests live in <c>Bodu.Numerics/test/Numerics/BigDecimalTests.*</c>.
/// </summary>
[TestClass]
public sealed class BigDecimalParseFormatContractTests
    : ParseFormatContractTests<BigDecimal>
{
    /// <inheritdoc />
    protected override BigDecimal Parse(string text, string? format = null, IFormatProvider? provider = null) =>
        BigDecimal.Parse(text, provider ?? CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override bool TryParse(string text, string? format, IFormatProvider? provider, out BigDecimal value) =>
        BigDecimal.TryParse(text, provider ?? CultureInfo.InvariantCulture, out value);

    /// <inheritdoc />
    protected override string Format(BigDecimal value, string? format = null, IFormatProvider? provider = null) =>
        value.ToString(format, provider ?? CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override IReadOnlyList<ValidKat<string, BigDecimal>> ValidCases { get; } =
        [
        new("zero",            Input: "0",            Expected: BigDecimal.Zero),
        new("integer",         Input: "100",          Expected: new BigDecimal(new BigInteger(100))),
        new("one half",        Input: "0.5",          Expected: new BigDecimal(new BigInteger(5), 1)),
        new("trailing zeros",  Input: "1.50",         Expected: new BigDecimal(new BigInteger(15), 1)),
        new("negative",        Input: "-3.14",        Expected: new BigDecimal(new BigInteger(-314), 2)),
        new("leading dot",     Input: ".25",          Expected: new BigDecimal(new BigInteger(25), 2)),
        new("scientific",      Input: "1.2e3",        Expected: new BigDecimal(new BigInteger(1200))),
        new("negative exp",    Input: "5e-2",         Expected: new BigDecimal(new BigInteger(5), 2)),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<InvalidKat<string>> InvalidCases { get; } =
        [
        new("not a number",      Input: "abc",     ExceptionType: typeof(FormatException)),
        new("empty",             Input: "",        ExceptionType: typeof(FormatException)),
        new("bare dot",          Input: ".",       ExceptionType: typeof(FormatException)),
        new("embedded letter",   Input: "1.2x",    ExceptionType: typeof(FormatException)),
        new("bad exponent",      Input: "1e",      ExceptionType: typeof(FormatException)),
    ];
}
