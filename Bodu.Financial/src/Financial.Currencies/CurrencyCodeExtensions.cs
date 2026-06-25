// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyCodeExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Frozen;
using System.Reflection;

namespace Bodu.Financial.Currencies;

/// <summary>
/// Provides lifecycle-status queries over <see cref="CurrencyCode" /> members.
/// </summary>
public static class CurrencyCodeExtensions
{
    /// <summary>Caches the <see cref="CurrencyStatus" /> of every <see cref="CurrencyCode" /> member, read once from its <see cref="CurrencyStatusAttribute" />.</summary>
    private static readonly FrozenDictionary<CurrencyCode, CurrencyStatus> s_status = BuildStatusMap();

    /// <summary>
    /// Gets the lifecycle status of the specified currency.
    /// </summary>
    /// <param name="code">The currency to classify.</param>
    /// <returns>
    /// The <see cref="CurrencyStatus" /> declared for <paramref name="code" />, or <see cref="CurrencyStatus.None" />
    /// when the value is not a defined member.
    /// </returns>
    public static CurrencyStatus GetStatus(this CurrencyCode code) =>
        s_status.TryGetValue(code, out CurrencyStatus status) ? status : CurrencyStatus.None;

    /// <summary>
    /// Returns a value indicating whether the specified currency is a historic (demonetized) currency.
    /// </summary>
    /// <param name="code">The currency to test.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="code" /> is historic; otherwise <see langword="false" />.
    /// </returns>
    public static bool IsHistoric(this CurrencyCode code) =>
        (GetStatus(code) & CurrencyStatus.Historic) != CurrencyStatus.None;

    /// <summary>
    /// Returns a value indicating whether the specified currency is an active currency.
    /// </summary>
    /// <param name="code">The currency to test.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="code" /> is active; otherwise <see langword="false" />.
    /// </returns>
    public static bool IsActive(this CurrencyCode code) =>
        (GetStatus(code) & CurrencyStatus.Active) != CurrencyStatus.None;

    /// <summary>
    /// Builds the immutable currency-to-status map by reflecting the <see cref="CurrencyStatusAttribute" /> on each
    /// <see cref="CurrencyCode" /> member.
    /// </summary>
    /// <returns>A frozen dictionary keyed by <see cref="CurrencyCode" />.</returns>
    private static FrozenDictionary<CurrencyCode, CurrencyStatus> BuildStatusMap()
    {
        var map = new Dictionary<CurrencyCode, CurrencyStatus>();
        foreach (FieldInfo field in typeof(CurrencyCode).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var code = (CurrencyCode)field.GetValue(null) !;
            CurrencyStatusAttribute? attribute = field.GetCustomAttribute<CurrencyStatusAttribute>();
            map[code] = attribute?.Status ?? CurrencyStatus.None;
        }

        return map.ToFrozenDictionary();
    }
}
