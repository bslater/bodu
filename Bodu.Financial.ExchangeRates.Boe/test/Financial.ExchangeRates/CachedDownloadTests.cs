// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachedDownloadTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies <see cref="CachedDownload" />, the cache lookup, download and store shared by the file-feed table sources.
/// </summary>
[TestClass]
public sealed partial class CachedDownloadTests
{
    /// <summary>The address the tests download from.</summary>
    private static readonly Uri s_url = new("https://feed.test/unit.csv");

    /// <summary>
    /// A byte cache that records how it is used and reports a fixed stored time for each hit.
    /// </summary>
    private sealed class RecordingCache
        : IByteCache<string>
    {
        /// <summary>The stored entries.</summary>
        private readonly Dictionary<string, byte[]> _entries = new(StringComparer.Ordinal);

        /// <summary>Gets the time the cache reports a hit was stored at.</summary>
        /// <value>A fixed instant.</value>
        public DateTimeOffset StoredAtUtc { get; } = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

        /// <summary>Gets the refresh interval of the last lookup.</summary>
        /// <value>The interval, or <see langword="null" /> before any lookup.</value>
        public TimeSpan? LastRefreshInterval { get; private set; }

        /// <summary>Gets the number of stores.</summary>
        /// <value>The store count.</value>
        public int StoreCount { get; private set; }

        /// <summary>Adds an entry without counting it as a store.</summary>
        /// <param name="key">The unit.</param>
        /// <param name="bytes">The bytes.</param>
        public void Seed(string key, byte[] bytes) => _entries[key] = bytes;

        /// <summary>Returns a stored entry.</summary>
        /// <param name="key">The unit.</param>
        /// <returns>The bytes, or <see langword="null" />.</returns>
        public byte[]? Peek(string key) => _entries.GetValueOrDefault(key);

        /// <inheritdoc />
        public bool TryGet(string key, TimeSpan refreshInterval, [MaybeNullWhen(false)] out byte[] bytes) =>
            TryGet(key, refreshInterval, out bytes, out _);

        /// <inheritdoc />
        public bool TryGet(string key, TimeSpan refreshInterval, [MaybeNullWhen(false)] out byte[] bytes, out DateTimeOffset? storedAtUtc)
        {
            LastRefreshInterval = refreshInterval;
            bool hit = _entries.TryGetValue(key, out bytes);
            storedAtUtc = hit ? StoredAtUtc : null;
            return hit;
        }

        /// <inheritdoc />
        public void Store(string key, byte[] bytes)
        {
            StoreCount++;
            _entries[key] = bytes;
        }
    }
}
