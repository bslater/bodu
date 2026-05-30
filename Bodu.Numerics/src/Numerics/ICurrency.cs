// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICurrency.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

/// <summary>
/// Describes the static, type-level metadata that identifies a currency for use as the <c>TCurrency</c> type parameter
/// of <see cref="Money{TCurrency}" />.
/// </summary>
/// <remarks>
/// <para>
/// Implementations are expected to be empty marker types — typically <see langword="sealed" /> classes or
/// <see langword="struct" />s — that exist only to bind a currency's ISO-style code and natural decimal-place count to
/// the type system. The static abstract members are resolved at the call site by C# 11 generic constraints, so callers
/// access them as <c>Money&lt;Usd&gt;.IsoCode</c> without ever instantiating the marker.
/// </para>
/// </remarks>
public interface ICurrency
{
    /// <summary>
    /// Gets the three-character uppercase ASCII ISO-style code that identifies the currency.
    /// </summary>
    /// <returns>A three-character currency code such as <c>"USD"</c> or <c>"AUD"</c>.</returns>
    static abstract string IsoCode { get; }

    /// <summary>
    /// Gets the number of decimal places used when rounding monetary amounts expressed in this currency.
    /// </summary>
    /// <returns>A non-negative integer in the range <c>0..28</c>.</returns>
    static abstract int DecimalPlaces { get; }
}
