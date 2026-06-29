// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateHistoryAvailabilityKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Identifies how an <see cref="ExchangeRateHistoryAvailability" /> bounds the earliest date for which a provider can
/// serve rates.
/// </summary>
public enum ExchangeRateHistoryAvailabilityKind
{
    /// <summary>
    /// The provider advertises no known lower bound on the dates it can serve.
    /// </summary>
    Unbounded,

    /// <summary>
    /// The provider serves only a rolling window of the most recent days, measured back from the current date.
    /// </summary>
    Rolling,

    /// <summary>
    /// The provider serves rates on or after a fixed calendar date.
    /// </summary>
    Since,
}
