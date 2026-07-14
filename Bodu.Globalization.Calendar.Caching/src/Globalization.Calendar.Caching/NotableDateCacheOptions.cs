// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Provides the storage-agnostic options shared by every <see cref="INotableDateCache" />. Storage-specific option
/// types derive from this base to add their own location settings.
/// </summary>
/// <remarks>
/// Unlike the exchange-rate cache, a notable-date cache is not bound to a single provider: it keys entries by
/// territory, year, and resource version, so one instance serves every territory a service resolves.
/// </remarks>
public class NotableDateCacheOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether a storage read or write failure is surfaced as an exception rather than
    /// degrading to an empty read or a skipped write.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to rethrow the underlying storage failure; <see langword="false" /> (the default) to
    /// keep the best-effort behaviour the <see cref="INotableDateCache" /> contract describes.
    /// </value>
    /// <remarks>
    /// The default keeps a cache fault from breaking notable-date resolution, which suits most consumers. Set it for a
    /// deployment that must not run with a silently broken cache. Argument validation always throws regardless of this
    /// setting.
    /// </remarks>
    public bool ThrowOnStorageFailure { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the cache eagerly probes its backing store when constructed and throws
    /// when the store is unusable, rather than deferring the discovery to the first read or write.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to probe the store at construction; <see langword="false" /> (the default) to skip the
    /// probe.
    /// </value>
    /// <remarks>
    /// The SQLite and distributed dependency-injection packages run this probe through their <c>ValidateOnStart</c>
    /// wiring, so a misconfigured store fails the host start; a cache constructed directly probes in its constructor
    /// instead. This setting is independent of <see cref="ThrowOnStorageFailure" />.
    /// </remarks>
    public bool ValidateStorageOnStart { get; set; }

    /// <summary>
    /// Validates the option values, throwing when a rule is violated.
    /// </summary>
    /// <remarks>
    /// The base has no invariants to enforce; storage-specific option types override this method to add their own. The
    /// throwing form preserves the <c>ParamName</c> of the offending option; the dependency-injection registration
    /// instead wires <see cref="TryValidate" /> into <c>ValidateOnStart</c>.
    /// </remarks>
    public virtual void Validate()
    {
    }

    /// <summary>
    /// Attempts to validate the options without throwing, reporting the first invariant that is violated.
    /// </summary>
    /// <param name="error">
    /// When this method returns <see langword="false" />, a message describing the first violated invariant; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when every invariant holds; otherwise <see langword="false" />.</returns>
    /// <remarks>
    /// The dependency-injection registration wires this method into <c>ValidateOnStart</c> so misconfiguration fails
    /// fast at application startup. Storage-specific option types override this method to add their own invariants
    /// after invoking the base implementation.
    /// </remarks>
    public virtual bool TryValidate(out string? error)
    {
        error = null;
        return true;
    }
}
