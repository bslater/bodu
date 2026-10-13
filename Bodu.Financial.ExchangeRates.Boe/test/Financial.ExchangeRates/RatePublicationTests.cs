// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RatePublicationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies <see cref="RatePublication" />, the shared rule for which days of a download are final and when the rest
/// expires.
/// </summary>
[TestClass]
public sealed partial class RatePublicationTests
{
    /// <summary>The instant the downloads in these tests were obtained: 09:00 UTC on 4 January 2023.</summary>
    private static readonly DateTimeOffset s_fetchedAt = new(2023, 1, 4, 9, 0, 0, TimeSpan.Zero);
}
