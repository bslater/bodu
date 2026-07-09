// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateFeedTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies the coverage and selection behavior of <see cref="EcbRateFeed" />.
/// </summary>
[TestClass]
public partial class EcbRateFeedTests
{
    /// <summary>
    /// A fixed reference date used so coverage assertions are deterministic.
    /// </summary>
    private static readonly DateOnly s_asOf = new(2026, 6, 13);
}
