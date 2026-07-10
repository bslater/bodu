// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyResolution.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Currencies;

/// <summary>
/// Supplies the ambient <see cref="ICurrencyLookup" /> that the runtime-tagged <see cref="Money" /> and
/// <see cref="CalculatedMoney" /> consult when they resolve a currency by ISO code. The seam lets a host or test
/// substitute a custom currency catalogue without threading a lookup dependency through every value-type construction,
/// while leaving the default behaviour identical to a direct <see cref="CurrencyRegistry" /> lookup.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Current" /> resolves to the active scoped override, if any, otherwise the process-wide default. The
/// default is the registry-backed <see cref="CurrencyLookupService" />; replace it once at composition root via
/// <see cref="SetDefault(ICurrencyLookup)" /> (the <c>Bodu.Financial.DependencyInjection</c> integration does this), or
/// install a temporary, flow-scoped override for a test or a single logical operation via
/// <see cref="PushScoped(ICurrencyLookup)" />.
/// </para>
/// <para>
/// The scoped override is stored in an <see cref="System.Threading.AsyncLocal{T}" />, so it is isolated per
/// asynchronous control flow and does not leak across parallel test cases.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Financial;
/// using Bodu.Financial.Currencies;
///
/// // A lookup restricted to the currencies this system settles in (a delegating
/// // ICurrencyLookup decorator over CurrencyResolution.Current, for example).
/// ICurrencyLookup restricted = new RestrictedCurrencyLookup(CurrencyResolution.Current, "AUD", "USD", "EUR");
///
/// using (CurrencyResolution.PushScoped(restricted))
/// {
///     // Inside the scope, runtime Money resolves through the override:
///     // unsupported ISO codes now fail parsing and metadata resolution.
///     var accepted = Money.TryParse("THB 25.00", CultureInfo.InvariantCulture, out _);   // false
/// }
///
/// // Disposing the scope restores the previous lookup. At composition root, prefer
/// // SetDefault (or the DI package's provider.UseCurrencyResolution()) instead.
///]]>
/// </code>
/// </example>
/// </remarks>
public static class CurrencyResolution
{
    /// <summary>The flow-local override installed by <see cref="PushScoped(ICurrencyLookup)" />, when present.</summary>
    private static readonly AsyncLocal<ICurrencyLookup?> s_scoped = new();

    /// <summary>The process-wide default lookup, used whenever no scoped override is active.</summary>
    private static volatile ICurrencyLookup s_default = new CurrencyLookupService();

    /// <summary>
    /// Gets the currency lookup currently in effect: the active scoped override, or the process-wide default.
    /// </summary>
    /// <value>The effective <see cref="ICurrencyLookup" />.</value>
    public static ICurrencyLookup Current =>
        s_scoped.Value ?? s_default;

    /// <summary>
    /// Replaces the process-wide default currency lookup. Intended to be called once during application start-up.
    /// </summary>
    /// <param name="lookup">The lookup to install as the default.</param>
    /// <exception cref="ArgumentNullException"><paramref name="lookup" /> is <see langword="null" />.</exception>
    public static void SetDefault(ICurrencyLookup lookup)
    {
        ThrowHelper.ThrowIfNull(lookup);
        s_default = lookup;
    }

    /// <summary>
    /// Installs <paramref name="lookup" /> as a flow-scoped override until the returned token is disposed.
    /// </summary>
    /// <param name="lookup">The lookup to install for the current control flow.</param>
    /// <returns>A token that restores the previous override when disposed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="lookup" /> is <see langword="null" />.</exception>
    public static IDisposable PushScoped(ICurrencyLookup lookup)
    {
        ThrowHelper.ThrowIfNull(lookup);

        ICurrencyLookup? previous = s_scoped.Value;
        s_scoped.Value = lookup;
        return new ScopeReverter(previous);
    }

    /// <summary>
    /// Attempts to resolve a currency by ISO code through the ambient lookup, mirroring the nullable-out shape of
    /// <see cref="CurrencyRegistry.TryGet(string, out CurrencyInfo?)" />.
    /// </summary>
    /// <param name="isoCode">The ISO 4217 alphabetic code, or <see langword="null" />.</param>
    /// <param name="info">
    /// When this method returns <see langword="true" />, the matching currency; otherwise <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when a currency is resolved; otherwise <see langword="false" />.</returns>
    internal static bool TryGet(string? isoCode, out CurrencyInfo? info)
    {
        if (isoCode is not null && Current.TryByIsoCode(isoCode, out CurrencyInfo found))
        {
            info = found;
            return true;
        }

        info = null;
        return false;
    }

    /// <summary>
    /// Determines whether the ambient lookup contains an entry for <paramref name="isoCode" />.
    /// </summary>
    /// <param name="isoCode">The ISO 4217 alphabetic code, or <see langword="null" />.</param>
    /// <returns><see langword="true" /> when an entry exists; otherwise <see langword="false" />.</returns>
    internal static bool Contains(string? isoCode) =>
        isoCode is not null && Current.TryByIsoCode(isoCode, out _);

    /// <summary>
    /// Restores the previous scoped override when disposed.
    /// </summary>
    private sealed class ScopeReverter
        : IDisposable
    {
        /// <summary>The override that was active before the enclosing <see cref="PushScoped(ICurrencyLookup)" /> call.</summary>
        private readonly ICurrencyLookup? _previous;

        /// <summary>Guards against repeated restoration on multiple <see cref="Dispose" /> calls.</summary>
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScopeReverter" /> class.
        /// </summary>
        /// <param name="previous">The override to restore on disposal.</param>
        public ScopeReverter(ICurrencyLookup? previous)
        {
            _previous = previous;
        }

        /// <summary>
        /// Restores the previous scoped override.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            s_scoped.Value = _previous;
        }
    }
}
