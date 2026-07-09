// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateDateResolution.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Describes how an exchange-rate lookup should resolve a requested date when an exact match is unavailable.
/// </summary>
/// <remarks>
/// <para>
/// The selected resolution interacts with <see cref="RateLookupOptions.ToleranceDays" /> to bound how far a fallback
/// date may be from the requested date. <see cref="Exact" /> never falls back and therefore requires a tolerance of
/// zero.
/// </para>
/// <para>
/// For accounting and tax workflows where silently selecting a future rate is undesirable, prefer
/// <see cref="PreviousOnOrBefore" />.
/// </para>
/// </remarks>
public enum RateDateResolution
{
    /// <summary>
    /// Only a rate observed on the exact requested date is accepted.
    /// </summary>
    Exact = 0,

    /// <summary>
    /// The closest available rate whose date is less than or equal to the requested date is used.
    /// </summary>
    PreviousOnOrBefore = 1,

    /// <summary>
    /// The closest available rate whose date is greater than or equal to the requested date is used.
    /// </summary>
    NextOnOrAfter = 2,

    /// <summary>
    /// The rate whose date is closest to the requested date by absolute day distance is used; ties are rejected.
    /// </summary>
    Nearest = 3,

    /// <summary>
    /// The available rate whose date is closest to the requested date is used; if the previous and next dates are
    /// equally close, the previous date wins. When the two candidates have <i>different</i> distances, the closer one
    /// is selected regardless of preference — the preference applies only at exact ties.
    /// </summary>
    NearestPreferPrevious = 4,

    /// <summary>
    /// The available rate whose date is closest to the requested date is used; if the previous and next dates are
    /// equally close, the next date wins. When the two candidates have <i>different</i> distances, the closer one is
    /// selected regardless of preference — the preference applies only at exact ties.
    /// </summary>
    NearestPreferNext = 5,
}
