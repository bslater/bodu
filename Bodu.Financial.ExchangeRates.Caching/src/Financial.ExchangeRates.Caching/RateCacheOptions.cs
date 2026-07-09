// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCacheOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Provides the storage-agnostic options shared by every <see cref="IRateCache" />: the single provider a cache
/// instance is bound to. Storage-specific option types derive from this base to add their own location settings.
/// </summary>
/// <remarks>
/// A cache instance serves exactly one provider, so the provider name is fixed here at construction rather than
/// supplied on each call. This lets the provider identity participate in the storage layout (for example a per-provider
/// subdirectory) without ambiguity, and keeps the <see cref="IRateCache" /> surface free of a provider argument.
/// </remarks>
public class RateCacheOptions
{
    /// <summary>
    /// Gets or sets the name of the provider whose rates this cache stores.
    /// </summary>
    /// <value>The provider identifier the cache is bound to.</value>
    public string Provider { get; set; } = default!;

    /// <summary>
    /// Gets or sets a value indicating whether a storage read or write failure is surfaced as an exception rather than
    /// degrading to an empty read or a skipped write.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to rethrow the underlying storage failure; <see langword="false" /> (the default) to
    /// keep the best-effort behaviour the <see cref="IRateCache" /> contract describes.
    /// </value>
    /// <remarks>
    /// The default keeps a cache fault from breaking rate retrieval, which suits most consumers. Set it for a
    /// deployment that must not run with a silently broken cache: the underlying failure then propagates from the read
    /// or write so a caller fails fast rather than trusting an empty or stale result. Argument validation always throws
    /// regardless of this setting.
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
    /// The probe surfaces a misconfigured directory, an unopenable database, or an unreachable distributed cache at
    /// construction. The SQLite and distributed dependency-injection packages run this probe through their
    /// <c>ValidateOnStart</c> wiring, so a misconfigured store fails the host start; a cache constructed directly
    /// probes in its constructor instead. This setting is independent of <see cref="ThrowOnStorageFailure" />: a cache
    /// can validate once at construction yet still degrade best-effort on a later transient fault, or run best-effort
    /// at construction yet fail fast on every later read or write.
    /// </remarks>
    public bool ValidateStorageOnStart { get; set; }

    /// <summary>
    /// Validates the option values, throwing when a rule is violated.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <see cref="Provider" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <see cref="Provider" /> is empty or white space.</exception>
    /// <remarks>
    /// This throwing form preserves the <c>ParamName</c> of the offending option, which callers rely on. The
    /// dependency-injection registration instead wires <see cref="TryValidate" /> into <c>ValidateOnStart</c>, which
    /// reports the same invariants without throwing.
    /// </remarks>
    public virtual void Validate() =>
        ThrowHelper.ThrowIfNullOrWhiteSpace(Provider);

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
    /// fast at application startup. It mirrors the invariants of <see cref="Validate" /> but returns a message rather
    /// than throwing with a <c>ParamName</c>. Storage-specific option types override this method to add their own
    /// invariants after invoking the base implementation.
    /// </remarks>
    public virtual bool TryValidate(out string? error)
    {
        if (string.IsNullOrWhiteSpace(Provider))
        {
            error = CachingResourceStrings.Arg_Invalid_ProviderBlank;
            return false;
        }

        error = null;
        return true;
    }
}
