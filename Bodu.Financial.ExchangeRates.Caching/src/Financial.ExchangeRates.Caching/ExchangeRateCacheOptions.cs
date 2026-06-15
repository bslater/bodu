// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCacheOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Provides the storage-agnostic options shared by every <see cref="IExchangeRateCache" />: the single provider a cache
/// instance is bound to. Storage-specific option types derive from this base to add their own location settings.
/// </summary>
/// <remarks>
/// A cache instance serves exactly one provider, so the provider name is fixed here at construction rather than
/// supplied on each call. This lets the provider identity participate in the storage layout (for example a per-provider
/// subdirectory) without ambiguity, and keeps the <see cref="IExchangeRateCache" /> surface free of a provider
/// argument.
/// </remarks>
public class ExchangeRateCacheOptions
{
    /// <summary>
    /// Gets or sets the name of the provider whose rates this cache stores.
    /// </summary>
    /// <value>The provider identifier the cache is bound to.</value>
    /// <returns>The configured provider name.</returns>
    public string Provider { get; set; } = default!;

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
