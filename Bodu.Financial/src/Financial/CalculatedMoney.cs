// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoney.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

/// <summary>
/// Represents a high-precision, runtime-tagged monetary amount whose rounding is deferred until it is converted back to
/// a settlement <see cref="Money" />.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CalculatedMoney" /> is the intermediate value used when a chain of monetary calculations must avoid the
/// rounding error that per-step rounding of <see cref="Money" /> would accumulate — compound interest, unit-rate
/// products, tax apportionment, and similar. Arithmetic preserves the full <see cref="decimal" /> precision; rounding
/// happens once, at the settlement boundary, through <see cref="RoundToMoney(MonetaryContext?)" />.
/// </para>
/// <para>
/// The currency is identified at runtime by <see cref="CurrencyCode" />. Arithmetic preserves full precision; rounding
/// to a settlement <see cref="Money" /> happens at the boundary through <see cref="RoundToMoney(MonetaryContext?)" />.
/// </para>
/// <para>
/// <see cref="CalculatedMoney" /> carries <em>high-precision <see cref="decimal" /></em> arithmetic with deferred
/// rounding — it is not an exact rational type. Division such as one-third is held to <see cref="decimal" />'s 28-29
/// significant digits, not exactly. When a calculation must be mathematically exact (for example, apportioning by an
/// exact fraction before settlement), use the exact-rational escape hatches on the strongly typed form —
/// <see cref="Money{TCurrency}.FromFraction(Bodu.Numerics.Fraction{System.Numerics.BigInteger}, MidpointRounding)" />
/// and
/// <see cref="Money{TCurrency}.MultiplyExact(Bodu.Numerics.Fraction{System.Numerics.BigInteger}, MidpointRounding)" />
/// — which compute in <see cref="Bodu.Numerics.Fraction{T}" /> and round once at the settlement boundary. In short:
/// <see cref="Money" />/<see cref="Money{TCurrency}" /> are rounded settlement values, <see cref="CalculatedMoney" />
/// is deferred-rounding decimal, and the <c>Fraction</c> APIs are exact rational.
/// </para>
/// </remarks>
[DebuggerDisplay("{Amount} {IsoCodeOrEmpty,nq} (unrounded)")]
public readonly partial struct CalculatedMoney
{
    /// <summary>The unrounded amount in the major unit of the currency identified by <see cref="_code" />.</summary>
    private readonly decimal _amount;

    /// <summary>The currency identifying this value, or <see cref="CurrencyCode.None" /> for a default-initialised value.</summary>
    private readonly CurrencyCode _code;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculatedMoney" /> struct from an amount and currency, preserving
    /// the amount's full precision.
    /// </summary>
    /// <param name="amount">The unrounded monetary amount in the major unit.</param>
    /// <param name="code">The currency identifying this value.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="code" /> is <see cref="CurrencyCode.None" /> or is not a defined <see cref="CurrencyCode" />
    /// member.
    /// </exception>
    public CalculatedMoney(decimal amount, CurrencyCode code)
    {
        FinancialThrowHelper.ThrowIfNotDefinedCurrencyCode(code);

        _amount = amount;
        _code = code;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculatedMoney" /> struct from pre-validated field values.
    /// </summary>
    /// <param name="amount">The unrounded amount.</param>
    /// <param name="code">The currency, or <see cref="CurrencyCode.None" /> for a currency-less value.</param>
    /// <param name="_">Discriminator that selects the pre-validated construction path.</param>
    private CalculatedMoney(decimal amount, CurrencyCode code, bool _)
    {
        _amount = amount;
        _code = code;
    }

    /// <summary>
    /// Returns a copy of this value with a different amount, preserving the currency.
    /// </summary>
    /// <param name="amount">The replacement unrounded amount.</param>
    /// <returns>The updated <see cref="CalculatedMoney" />.</returns>
    private CalculatedMoney WithAmount(decimal amount) =>
        new(amount, _code, false);
}
