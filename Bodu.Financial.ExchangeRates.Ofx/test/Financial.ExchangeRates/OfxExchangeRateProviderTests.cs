// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OfxExchangeRateProviderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the construction behavior of <see cref="OfxExchangeRateProvider" /> that the shared dated-provider
/// contract tests do not exercise, notably the public options-only constructor that builds and owns its
/// <see cref="HttpClient" />.
/// </summary>
[TestClass]
public partial class OfxExchangeRateProviderTests
{
}
