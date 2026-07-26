// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComplexParseFormatContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.Numerics.Contracts;

/// <summary>
/// Drives <see cref="ParseFormatContractTests{T}" /> against <see cref="Complex{T}" /> instantiated over
/// <see cref="double" />. The contract base covers Parse(success), Parse(failure throws FormatException),
/// TryParse(failure returns false), and the parse → format → parse round trip. The overrides pin the invariant culture
/// so the component separator is deterministic regardless of the test host culture. Bespoke Complex tests live in
/// <c>Bodu.Numerics/test/Numerics/ComplexTests.*</c>.
/// </summary>
[TestClass]
public sealed class ComplexParseFormatContractTests
    : ParseFormatContractTests<Complex<double>>
{
    /// <inheritdoc />
    protected override Complex<double> Parse(string text, string? format = null, IFormatProvider? provider = null) =>
        Complex<double>.Parse(text, provider ?? CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override bool TryParse(string text, string? format, IFormatProvider? provider, out Complex<double> value) =>
        Complex<double>.TryParse(text, provider ?? CultureInfo.InvariantCulture, out value);

    /// <inheritdoc />
    protected override string Format(Complex<double> value, string? format = null, IFormatProvider? provider = null) =>
        value.ToString(format, provider ?? CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override IReadOnlyList<ValidKat<string, Complex<double>>> ValidCases { get; } =
        [
        new("zero",              Input: "<0; 0>",     Expected: Complex<double>.Zero),
        new("real and imaginary", Input: "<3; 4>",    Expected: new Complex<double>(3.0, 4.0)),
        new("negative imaginary", Input: "<3; -4>",   Expected: new Complex<double>(3.0, -4.0)),
        new("negative real",      Input: "<-2.5; 1>", Expected: new Complex<double>(-2.5, 1.0)),
        new("bare real",          Input: "5",         Expected: new Complex<double>(5.0, 0.0)),
        new("imaginary unit",     Input: "<0; 1>",    Expected: Complex<double>.ImaginaryOne),
    ];

    /// <inheritdoc />
    protected override IReadOnlyList<InvalidKat<string>> InvalidCases { get; } =
        [
        new("not a complex",       Input: "abc",       ExceptionType: typeof(FormatException)),
        new("empty",               Input: "",          ExceptionType: typeof(FormatException)),
        new("missing close",       Input: "<1; 2",     ExceptionType: typeof(FormatException)),
        new("missing separator",   Input: "<1 2>",     ExceptionType: typeof(FormatException)),
        new("missing imaginary",   Input: "<3; >",     ExceptionType: typeof(FormatException)),
        new("non-numeric component", Input: "<1; x>",  ExceptionType: typeof(FormatException)),
    ];
}
