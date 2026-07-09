// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateDateResolutionKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Financial.ExchangeRates.Kat;

/// <summary>
/// Represents a single known-answer test row exercising <see cref="RateSeries.TryGetRate" /> over a synthetic
/// series whose dates are supplied directly, focusing on date-resolution behaviour rather than rate value.
/// </summary>
/// <param name="Name">The short label identifying the scenario.</param>
/// <param name="RequestedDate">The date the caller asks about.</param>
/// <param name="Resolution">The date-resolution policy in effect.</param>
/// <param name="ToleranceDays">The tolerance value supplied through <see cref="RateLookupOptions" />.</param>
/// <param name="AvailableDates">The observation dates in the series under test.</param>
/// <param name="ExpectedDate">
/// The expected resolved date, or <see langword="null" /> when <paramref name="ExpectedSuccess" /> is
/// <see langword="false" />.
/// </param>
/// <param name="ExpectedSuccess">
/// <see langword="true" /> when the lookup should succeed; otherwise <see langword="false" />.
/// </param>
public sealed record RateDateResolutionKat(
    string Name,
    DateOnly RequestedDate,
    RateDateResolution Resolution,
    int ToleranceDays,
    DateOnly[] AvailableDates,
    DateOnly? ExpectedDate,
    bool ExpectedSuccess) : IKat;
