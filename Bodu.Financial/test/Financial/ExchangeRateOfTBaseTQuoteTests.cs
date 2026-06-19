// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateOfTBaseTQuoteTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

/// <summary>
/// Verifies the strongly-typed <see cref="ExchangeRate{TBase, TQuote}" /> companion of <see cref="ExchangeRate" />,
/// including construction, the typed <c>Inverse</c> / <c>ToRuntime</c> / <c>FromRuntime</c> bridges, and the
/// compile-time-direction-checked conversion on <see cref="Money{TCurrency}" />.
/// </summary>
[TestClass]
public partial class ExchangeRateOfTBaseTQuoteTests
{
    private static readonly DateOnly SampleDate = new(2026, 1, 15);
    private const string SampleProvider = "TEST";
}
