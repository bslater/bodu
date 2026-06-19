// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbExchangeRateFeedTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Ecb;

/// <summary>
/// Verifies the coverage and selection behavior of <see cref="EcbExchangeRateFeed" />.
/// </summary>
[TestClass]
public partial class EcbExchangeRateFeedTests
{
    /// <summary>
    /// A fixed reference date used so coverage assertions are deterministic.
    /// </summary>
    private static readonly DateOnly s_asOf = new(2026, 6, 13);
}
