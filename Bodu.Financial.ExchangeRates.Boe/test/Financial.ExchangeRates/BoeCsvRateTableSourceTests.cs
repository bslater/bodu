// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeCsvRateTableSourceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Bodu.Financial.ExchangeRates.Testing;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies how <see cref="BoeCsvRateTableSource" /> consults its response cache and the IADB endpoint.
/// </summary>
[TestClass]
public sealed partial class BoeCsvRateTableSourceTests
{
    /// <summary>The inclusive start of the range the tests request.</summary>
    private static readonly DateOnly s_from = new(2023, 1, 1);

    /// <summary>The inclusive end of the range the tests request.</summary>
    private static readonly DateOnly s_to = new(2023, 1, 31);

    /// <summary>
    /// Creates a source over a stub handler serving the sample response and a recording cache.
    /// </summary>
    /// <param name="handler">The stub handler.</param>
    /// <param name="cache">The recording cache.</param>
    /// <param name="options">The provider options, or <see langword="null" /> for the defaults.</param>
    /// <returns>The source.</returns>
    private static BoeCsvRateTableSource CreateSource(out StubHttpMessageHandler handler, out RecordingCache cache, BoeRateProviderOptions? options = null)
    {
        handler = new StubHttpMessageHandler(BoeFixtures.ReadBytes(BoeFixtures.Sample));
        cache = new RecordingCache();

        return new BoeCsvRateTableSource(new HttpClient(handler), options ?? new BoeRateProviderOptions(), cache);
    }

    /// <summary>
    /// An in-memory response cache that records how it is used.
    /// </summary>
    private sealed class RecordingCache
        : IByteCache<(DateOnly StartDate, DateOnly EndDate)>
    {
        /// <summary>The stored entries.</summary>
        private readonly Dictionary<(DateOnly StartDate, DateOnly EndDate), byte[]> _entries = [];

        /// <summary>
        /// Gets the number of stores.
        /// </summary>
        /// <value>The store count.</value>
        public int StoreCount { get; private set; }

        /// <summary>
        /// Gets the refresh interval of the last lookup.
        /// </summary>
        /// <value>The interval, or <see langword="null" /> before any lookup.</value>
        public TimeSpan? LastRefreshInterval { get; private set; }

        /// <summary>
        /// Adds an entry without counting it as a store.
        /// </summary>
        /// <param name="key">The range.</param>
        /// <param name="bytes">The bytes.</param>
        public void Seed((DateOnly StartDate, DateOnly EndDate) key, byte[] bytes) => _entries[key] = bytes;

        /// <inheritdoc />
        public bool TryGet((DateOnly StartDate, DateOnly EndDate) key, TimeSpan refreshInterval, [MaybeNullWhen(false)] out byte[] bytes)
        {
            LastRefreshInterval = refreshInterval;
            return _entries.TryGetValue(key, out bytes);
        }

        /// <inheritdoc />
        public void Store((DateOnly StartDate, DateOnly EndDate) key, byte[] bytes)
        {
            StoreCount++;
            _entries[key] = bytes;
        }
    }
}
