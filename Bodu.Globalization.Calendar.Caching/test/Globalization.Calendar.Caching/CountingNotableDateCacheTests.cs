// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountingNotableDateCacheTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies the <see cref="CountingNotableDateCache" /> test double, whose read counts the caching tests assert while
/// the service under test reads years on several threads at once.
/// </summary>
[TestClass]
public sealed partial class CountingNotableDateCacheTests
{
    /// <summary>The instant the reads are made at.</summary>
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>The time-to-live the reads evaluate freshness against.</summary>
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(30);
}
