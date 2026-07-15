// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCacheWarmupOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Configures the startup cache warm-up registered through <c>AddRateCacheWarmup</c>: the currency pairs to warm and
/// the date window they are warmed over.
/// </summary>
/// <remarks>
/// <para>
/// The warmed window defaults to a rolling look-back from the current UTC date — <c>[today −
/// <see cref="LookbackDays" />, today]</c>, evaluated when the warm-up runs — so a service that restarts daily always
/// warms its recent history. Either bound can be pinned with <see cref="StartDate" /> or <see cref="EndDate" />; a
/// pinned bound replaces its rolling default independently of the other.
/// </para>
/// <para>
/// Pairs are ISO 4217 code pairs in <c>"XXX/YYY"</c> form, binding cleanly from configuration arrays. Validation checks
/// the shape only; a well-formed pair whose code is unknown to the currency registry surfaces at warm-up time as a
/// logged, skipped pair rather than failing application start.
/// </para>
/// </remarks>
public sealed class RateCacheWarmupOptions
{
    /// <summary>
    /// Gets the currency pairs to warm, each in <c>"XXX/YYY"</c> ISO 4217 form.
    /// </summary>
    /// <value>
    /// The pair list; empty by default, which fails validation so an unconfigured warm-up cannot register.
    /// </value>
    public IList<string> Pairs { get; } = new List<string>();

    /// <summary>
    /// Gets or sets the number of days before the current UTC date the warmed window reaches back, used when
    /// <see cref="StartDate" /> is not set.
    /// </summary>
    /// <value>The rolling look-back, in days; defaults to 30. Must not be negative.</value>
    public int LookbackDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets the fixed inclusive first date of the warmed window, replacing the rolling
    /// <see cref="LookbackDays" /> start.
    /// </summary>
    /// <value>
    /// The fixed start date, or <see langword="null" /> to derive the start from <see cref="LookbackDays" />.
    /// </value>
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the fixed inclusive last date of the warmed window, replacing the rolling current-date end.
    /// </summary>
    /// <value>The fixed end date, or <see langword="null" /> to end the window at the current UTC date.</value>
    public DateOnly? EndDate { get; set; }

    /// <summary>
    /// Gets the names of keyed aggregation children to warm in addition to every directly registered caching provider.
    /// </summary>
    /// <value>
    /// The keyed child names, matching the names supplied to <c>AddAggregatedRateProvider</c>; empty by default. A name
    /// that resolves to no keyed caching provider is logged and skipped at warm-up time.
    /// </value>
    public IList<string> Providers { get; } = new List<string>();

    /// <summary>
    /// Validates the option values, throwing when a rule is violated.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="Pairs" /> is empty or contains a malformed pair, when <see cref="LookbackDays" /> is
    /// negative, or when <see cref="StartDate" /> and <see cref="EndDate" /> are both set and inverted.
    /// </exception>
    /// <remarks>
    /// This throwing form preserves the <c>ParamName</c> of the offending option. The dependency-injection registration
    /// instead wires <see cref="TryValidate" /> into <c>ValidateOnStart</c>.
    /// </remarks>
    public void Validate()
    {
        if (Pairs is null || Pairs.Count == 0)
            throw new ArgumentException(CachingResourceStrings.Arg_Invalid_WarmupPairsEmpty, nameof(Pairs));

        foreach (string pair in Pairs)
        {
            if (!TryParsePair(pair, out _))
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture, CachingResourceStrings.Arg_Invalid_WarmupPairMalformed, pair),
                    nameof(Pairs));
            }
        }

        if (LookbackDays < 0)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, CachingResourceStrings.Arg_Invalid_WarmupLookbackNegative, LookbackDays),
                nameof(LookbackDays));
        }

        if (StartDate is { } start && EndDate is { } end && end < start)
            throw new ArgumentException(CachingResourceStrings.Arg_Invalid_WarmupWindowInverted, nameof(EndDate));
    }

    /// <summary>
    /// Attempts to validate the options without throwing, reporting the first invariant that is violated.
    /// </summary>
    /// <param name="error">
    /// When this method returns <see langword="false" />, a message describing the first violated invariant; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when every invariant holds; otherwise <see langword="false" />.</returns>
    public bool TryValidate(out string? error)
    {
        if (Pairs is null || Pairs.Count == 0)
        {
            error = CachingResourceStrings.Arg_Invalid_WarmupPairsEmpty;
            return false;
        }

        foreach (string pair in Pairs)
        {
            if (!TryParsePair(pair, out _))
            {
                error = string.Format(CultureInfo.CurrentCulture, CachingResourceStrings.Arg_Invalid_WarmupPairMalformed, pair);
                return false;
            }
        }

        if (LookbackDays < 0)
        {
            error = string.Format(CultureInfo.CurrentCulture, CachingResourceStrings.Arg_Invalid_WarmupLookbackNegative, LookbackDays);
            return false;
        }

        if (StartDate is { } start && EndDate is { } end && end < start)
        {
            error = CachingResourceStrings.Arg_Invalid_WarmupWindowInverted;
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Attempts to split a configured pair string into its from and to ISO codes, accepting only the <c>"XXX/YYY"</c>
    /// shape of two three-letter codes.
    /// </summary>
    /// <param name="value">The configured pair string.</param>
    /// <param name="pair">When this method returns <see langword="true" />, the split ISO codes.</param>
    /// <returns>
    /// <see langword="true" /> when the string has the expected shape; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Only the shape is checked here, so validation stays registry-independent; whether each code names a known
    /// currency is decided when the warm-up actually resolves it.
    /// </remarks>
    internal static bool TryParsePair(string? value, out (string From, string To) pair)
    {
        pair = default;

        if (value is null || value.Length != 7 || value[3] != '/')
            return false;

        for (int i = 0; i < value.Length; i++)
        {
            if (i != 3 && !char.IsAsciiLetter(value[i]))
                return false;
        }

        pair = (value[..3].ToUpperInvariant(), value[4..].ToUpperInvariant());
        return true;
    }
}
