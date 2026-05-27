// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionParseFormatContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.Numerics.Contracts;

/// <summary>
/// Drives <see cref="ParseFormatContractTests{T}" /> against <see cref="Fraction{T}" /> instantiated
/// over <see cref="int" />. The contract base covers Parse(success), Parse(failure throws
/// FormatException), TryParse(failure returns false), and the parse → format → parse round trip.
/// Bespoke Fraction tests (overflow, unsigned backing, generic math) live in
/// <c>Bodu.Numerics/test/Numerics/FractionTests.*</c>.
/// </summary>
[TestClass]
public sealed class FractionParseFormatContractTests : ParseFormatContractTests<Fraction<int>>
{
    /// <inheritdoc />
    protected override Fraction<int> Parse(string text, string? format = null, IFormatProvider? provider = null) =>
        Fraction<int>.Parse(text, provider);

    /// <inheritdoc />
    protected override bool TryParse(string text, string? format, IFormatProvider? provider, out Fraction<int> value) =>
        Fraction<int>.TryParse(text, provider, out value);

    /// <inheritdoc />
    protected override string Format(Fraction<int> value, string? format = null, IFormatProvider? provider = null) =>
        value.ToString(format, provider);

    /// <inheritdoc />
    protected override IReadOnlyList<ValidKat<string, Fraction<int>>> ValidCases { get; } =
        [
        new("zero",          Input: "0",        Expected: new Fraction<int>(0, 1)),
        new("one half",      Input: "1/2",      Expected: new Fraction<int>(1, 2)),
        new("five eighths",  Input: "5/8",      Expected: new Fraction<int>(5, 8)),
        new("negative",      Input: "-3/4",     Expected: new Fraction<int>(-3, 4)),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<InvalidKat<string>> InvalidCases { get; } =
        [
        new("not a fraction",        Input: "abc",      ExceptionType: typeof(FormatException)),
        new("denominator missing",   Input: "1/",       ExceptionType: typeof(FormatException)),
        new("numerator missing",     Input: "/2",       ExceptionType: typeof(FormatException)),
        new("denominator non-numeric", Input: "1/x",    ExceptionType: typeof(FormatException)),
    ];
}
