// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheWriteStatus.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Reports the outcome of an
/// <see cref="INotableDateCache.StoreYear(NotableDateCacheEntry, TimeSpan, DateTimeOffset)" /> write, distinguishing a
/// durable success from a swallowed storage failure and from a deliberate no-op cache.
/// </summary>
/// <remarks>
/// The status exists so a caller can tell whether the computed year was actually persisted. A <see cref="Failed" />
/// result signals that nothing was persisted (a backing-store error was swallowed to keep the cache best-effort), so a
/// subsequent lookup of the same year recomputes rather than trusting a write that never landed.
/// </remarks>
public enum NotableDateCacheWriteStatus
{
    /// <summary>
    /// The computed year was persisted; the entry is durable for the backend's lifetime.
    /// </summary>
    Stored,

    /// <summary>
    /// The cache intentionally stored nothing, as a no-op cache does; nothing was persisted and nothing was expected to
    /// be.
    /// </summary>
    Skipped,

    /// <summary>
    /// A storage error was swallowed and nothing was persisted; the entry is not durable.
    /// </summary>
    Failed,
}
