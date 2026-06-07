// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.OfTCurrency.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Text.Json.Serialization;
using Bodu.Financial.Serialization;

namespace Bodu.Financial;

/// <summary>
/// Represents an immutable monetary amount denominated in the currency identified by <typeparamref name="TCurrency" />.
/// </summary>
/// <typeparam name="TCurrency">
/// A type implementing <see cref="ICurrency" /> that identifies the currency at the type-system level. Use one of the
/// shipped tag types in <c>Bodu.Financial.Currencies</c> (for example,
/// <c>Money&lt;Bodu.Financial.Currencies.USD&gt;</c>).
/// </typeparam>
/// <remarks>
/// <para>
/// <see cref="Money{TCurrency}" /> represents a <b>settlement-grade</b> monetary value — an amount already rounded to
/// the currency's minor-unit precision and ready to be posted to a ledger, displayed to a user, or serialized to
/// storage. It is deliberately not a calculation type: every operation that produces a <see cref="Money{TCurrency}" />
/// returns a value that respects <c>TCurrency.MinorUnits</c>, so chained scalar operations round at each step rather
/// than accumulating sub-minor-unit precision.
/// </para>
/// <para>
/// The amount is stored as a <see cref="decimal" /> rounded on construction to <c>TCurrency.MinorUnits</c> using
/// banker's rounding (<see cref="MidpointRounding.ToEven" />). Two <see cref="Money{TCurrency}" /> values that
/// represent the same monetary amount compare equal regardless of the input expressions that produced them.
/// </para>
/// <para>
/// Arithmetic between two <see cref="Money{TCurrency}" /> instances is permitted only when both operands share the same
/// <typeparamref name="TCurrency" />. Cross-currency addition, subtraction, and comparison are compile errors, not
/// runtime exceptions; cross-currency conversion is available exclusively through the explicit
/// <see cref="Convert{TTarget}(decimal, MidpointRounding)" /> method.
/// </para>
/// <para>
/// Scalar multiplication and division round their result to <c>TCurrency.MinorUnits</c>. For chains where rounding at
/// each step would accumulate error — compound interest, unit-rate products, percentages — defer rounding: use
/// <see cref="CalculatedMoney" /> for high-precision <see cref="decimal" /> calculation rounded once at settlement, or,
/// when the calculation must be mathematically exact, perform it in <see cref="Bodu.Numerics.Fraction{T}" /> via
/// <see cref="ToFraction" /> and snap to <see cref="Money{TCurrency}" /> only at the final settlement boundary with
/// <see cref="FromFraction(Bodu.Numerics.Fraction{System.Numerics.BigInteger}, MidpointRounding)" /> or the
/// <see cref="MultiplyExact(Bodu.Numerics.Fraction{System.Numerics.BigInteger}, MidpointRounding)" /> shortcut.
/// </para>
/// <para>
/// The default value (<c>default(Money&lt;TCurrency&gt;)</c>) represents zero of the given currency without invoking
/// the rounding constructor, so it is safe even when <typeparamref name="TCurrency" /> reports invalid minor-unit
/// metadata.
/// </para>
/// <para>
/// The type-level <see cref="JsonConverterAttribute" /> always serializes the canonical
/// <see cref="Serialization.FinancialJsonPolicy.Strict" /> object shape; the compact and lenient shapes are opt-in only
/// and must be selected through
/// <see cref="Serialization.FinancialJsonSerializerOptionsExtensions.AddFinancialJsonConverters(System.Text.Json.JsonSerializerOptions, Serialization.FinancialJsonPolicy)" />
/// .
/// </para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
[JsonConverter(typeof(MoneyOfTCurrencyJsonConverterFactory))]
public readonly partial struct Money<TCurrency>
    where TCurrency : ICurrency
{
    /// <summary>
    /// The rounded amount in the major unit of <typeparamref name="TCurrency" />.
    /// </summary>
    private readonly decimal _amount;

    /// <summary>
    /// Initializes a new instance of the <see cref="Money{TCurrency}" /> struct, rounding <paramref name="amount" /> to
    /// <c>TCurrency.MinorUnits</c> using banker's rounding.
    /// </summary>
    /// <param name="amount">The monetary amount in the major unit of <typeparamref name="TCurrency" />.</param>
    /// <remarks>
    /// <para>
    /// The supplied amount is rounded to the currency's minor-unit precision using banker's rounding — midpoint values
    /// round toward the nearer even final digit, in both directions. For example, <c>new Money&lt;USD&gt;(1.225m)</c>
    /// is stored as <c>1.22m</c> (down toward the even hundredth <c>2</c>) while <c>new Money&lt;USD&gt;(1.235m)</c> is
    /// stored as <c>1.24m</c> (up toward the even hundredth <c>4</c>). Non-midpoint values round in the natural
    /// direction: <c>new Money&lt;JPY&gt;(99.6m)</c> is stored as <c>100m</c>.
    /// </para>
    /// <para>
    /// To round with a different rule, use the <see cref="Money{TCurrency}(decimal, MidpointRounding)" /> overload.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <typeparamref name="TCurrency" /> reports invalid metadata — an <see cref="ICurrency.IsoCode" />
    /// that is not exactly three uppercase ASCII letters, an <see cref="ICurrency.MinorUnits" /> outside the inclusive
    /// range <c>[0, 28]</c>, or a <see cref="ICurrency.CashRoundingIncrement" /> that is negative or finer than the
    /// currency's minor-unit precision.
    /// </exception>
    public Money(decimal amount)
    {
        _amount = MoneyMath.Round(amount, CurrencyMetadata<TCurrency>.Value.MinorUnits, MidpointRounding.ToEven);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Money{TCurrency}" /> struct, rounding <paramref name="amount" /> to
    /// <c>TCurrency.MinorUnits</c> using the specified midpoint-rounding rule.
    /// </summary>
    /// <param name="amount">The monetary amount in the major unit of <typeparamref name="TCurrency" />.</param>
    /// <param name="rounding">The rule used to round midpoint values.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <typeparamref name="TCurrency" /> reports invalid metadata.
    /// </exception>
    public Money(decimal amount, MidpointRounding rounding)
    {
        _amount = MoneyMath.Round(amount, CurrencyMetadata<TCurrency>.Value.MinorUnits, rounding);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Money{TCurrency}" /> struct from an already-rounded amount,
    /// bypassing normalization.
    /// </summary>
    /// <param name="amount">The pre-rounded monetary amount.</param>
    /// <param name="_">
    /// A typed tag that selects the no-normalization initialization path. The value itself is not inspected.
    /// </param>
    private Money(decimal amount, NormalizedTag _)
    {
        _amount = amount;
    }

    /// <summary>
    /// Creates a <see cref="Money{TCurrency}" /> from an amount that is already rounded to <c>TCurrency.MinorUnits</c>,
    /// bypassing the normalization step.
    /// </summary>
    /// <param name="amount">The already-normalized amount.</param>
    /// <returns>A <see cref="Money{TCurrency}" /> wrapping <paramref name="amount" />.</returns>
    /// <remarks>
    /// This is the internal fast path used by arithmetic operators, allocation, and conversion to avoid a redundant
    /// <see cref="decimal.Round(decimal, int, MidpointRounding)" /> after every operation when the caller already
    /// guarantees minor-unit precision. A <see cref="System.Diagnostics.Debug.Assert(bool, string)" /> guards the
    /// invariant in DEBUG builds so misuse fails noisily during development. External callers should use the public
    /// constructors.
    /// </remarks>
    internal static Money<TCurrency> FromNormalizedAmount(decimal amount)
    {
        System.Diagnostics.Debug.Assert(
            IsAtMinorUnitPrecision(amount),
            $"FromNormalizedAmount called with non-normalized amount {amount} for {typeof(TCurrency).Name} (MinorUnits={CurrencyMetadata<TCurrency>.Value.MinorUnits}).");
        return new(amount, default(NormalizedTag));
    }

    /// <summary>
    /// Determines whether <paramref name="amount" /> already sits at <typeparamref name="TCurrency" />'s minor-unit
    /// precision (no finer fractional digits).
    /// </summary>
    /// <param name="amount">The amount to test.</param>
    /// <returns><see langword="true" /> when the amount is at minor-unit precision.</returns>
    private static bool IsAtMinorUnitPrecision(decimal amount) =>
        MoneyMath.Round(amount, CurrencyMetadata<TCurrency>.Value.MinorUnits, MidpointRounding.ToEven) == amount;

    /// <summary>
    /// A typed discriminator that selects the no-normalization initialization path on the private
    /// <see cref="Money{TCurrency}" /> constructor invoked from <see cref="FromNormalizedAmount(decimal)" />.
    /// </summary>
    private readonly struct NormalizedTag
    {
    }
}
