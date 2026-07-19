// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InMemoryNotableDateCacheContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Caching;

namespace Bodu.Globalization.Calendar.Caching.Contracts;

/// <summary>
/// Runs the <see cref="NotableDateCacheContractTests" /> against <see cref="InMemoryNotableDateCache" />.
/// </summary>
[TestClass]
public sealed class InMemoryNotableDateCacheContractTests
    : NotableDateCacheContractTests
{
    /// <inheritdoc />
    protected override INotableDateCache CreateCache() => new InMemoryNotableDateCache();
}
