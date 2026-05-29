// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Money.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Bodu.Numerics;

/// <summary>
/// Represents an immutable monetary amount denominated in the currency identified by <typeparamref name="TCurrency" />.
/// </summary>
/// <typeparam name="TCurrency">
/// A type implementing <see cref="ICurrency" /> that identifies the currency at the type-system level. Use one of
/// the shipped tag types in <c>Bodu.Numerics.Currencies</c> (for example, <c>Money&lt;Bodu.Numerics.Currencies.USD&gt;</c>).
/// </typeparam>
/// <remarks>
/// <para>
/// The amount is stored as a <see cref="decimal" /> rounded on construction to <c>TCurrency.MinorUnits</c> using
/// banker's rounding (<see cref="MidpointRounding.ToEven" />). Two <see cref="Money{TCurrency}" /> values that
/// represent the same monetary amount compare equal regardless of the input expressions that produced them.
/// </para>
/// <para>
/// Arithmetic between two <see cref="Money{TCurrency}" /> instances is permitted only when both operands share the
/// same <typeparamref name="TCurrency" />. Cross-currency addition, subtraction, and comparison are compile errors,
/// not runtime exceptions; cross-currency conversion is available exclusively through the explicit
/// <see cref="Convert{TTarget}(decimal, MidpointRounding)" /> method.
/// </para>
/// <para>
/// Scalar multiplication and division round their result to <c>TCurrency.MinorUnits</c>. For chains where rounding
/// at each step would accumulate error — compound interest, unit-rate products, percentages — convert to
/// <see cref="Fraction{T}" /> with <see cref="ToFraction" />, perform the exact computation, and convert back with
/// <see cref="FromFraction(Fraction{System.Numerics.BigInteger}, MidpointRounding)" />.
/// </para>
/// <para>
/// The default value (<c>default(Money&lt;TCurrency&gt;)</c>) represents zero of the given currency.
/// </para>
/// </remarks>
[Serializable]
[DebuggerDisplay("{ToString(),nq}")]
[JsonConverter(typeof(MoneyJsonConverterFactory))]
public readonly partial struct Money<TCurrency>
    where TCurrency : ICurrency
{
    /// <summary>
    /// The rounded amount in the major unit of <typeparamref name="TCurrency" />.
    /// </summary>
    private readonly decimal _amount;

    /// <summary>
    /// Initializes a new instance of the <see cref="Money{TCurrency}" /> struct, rounding <paramref name="amount" />
    /// to <c>TCurrency.MinorUnits</c> using banker's rounding.
    /// </summary>
    /// <param name="amount">The monetary amount in the major unit of <typeparamref name="TCurrency" />.</param>
    /// <remarks>
    /// The supplied amount is rounded to the currency's minor-unit precision; for example,
    /// <c>new Money&lt;USD&gt;(1.235m)</c> is stored as <c>1.24m</c> and <c>new Money&lt;JPY&gt;(99.6m)</c> is stored
    /// as <c>100m</c>. To round with a different rule, use the
    /// <see cref="Money{TCurrency}(decimal, MidpointRounding)" /> overload.
    /// </remarks>
    public Money(decimal amount)
    {
        _amount = decimal.Round(amount, ValidatedMinorUnits, MidpointRounding.ToEven);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Money{TCurrency}" /> struct, rounding <paramref name="amount" />
    /// to <c>TCurrency.MinorUnits</c> using the specified midpoint-rounding rule.
    /// </summary>
    /// <param name="amount">The monetary amount in the major unit of <typeparamref name="TCurrency" />.</param>
    /// <param name="rounding">The rule used to round midpoint values.</param>
    public Money(decimal amount, MidpointRounding rounding)
    {
        _amount = decimal.Round(amount, ValidatedMinorUnits, rounding);
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
    /// Creates a <see cref="Money{TCurrency}" /> from an amount that is already rounded to
    /// <c>TCurrency.MinorUnits</c>, bypassing the normalization step.
    /// </summary>
    /// <param name="amount">The already-normalized amount.</param>
    /// <returns>A <see cref="Money{TCurrency}" /> wrapping <paramref name="amount" />.</returns>
    /// <remarks>
    /// This is the internal fast path used by arithmetic operators, allocation, and conversion to avoid a
    /// redundant <see cref="decimal.Round(decimal, int, MidpointRounding)" /> after every operation when the
    /// caller already guarantees minor-unit precision. External callers should use the public constructors.
    /// </remarks>
    internal static Money<TCurrency> FromNormalizedAmount(decimal amount) =>
        new(amount, default(NormalizedTag));

    /// <summary>
    /// Validates that <c>TCurrency.MinorUnits</c> is within the inclusive range <c>[0, 28]</c> that
    /// <see cref="decimal.Round(decimal, int, MidpointRounding)" /> accepts.
    /// </summary>
    /// <returns>The validated minor-unit precision.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <c>TCurrency.MinorUnits</c> is negative or greater than the 28-decimal-digit precision of
    /// <see cref="decimal" />. The exception message names the offending <typeparamref name="TCurrency" /> type.
    /// </exception>
    private static int ValidatedMinorUnits
    {
        get
        {
            int value = TCurrency.MinorUnits;
            if ((uint)value > 28u)
            {
                throw new InvalidOperationException(
                    $"{typeof(TCurrency).FullName}.{nameof(ICurrency.MinorUnits)} must be between 0 and 28, but reported {value}.");
            }

            return value;
        }
    }

    /// <summary>
    /// A typed discriminator that selects the no-normalization initialization path on the private
    /// <see cref="Money{TCurrency}" /> constructor invoked from <see cref="FromNormalizedAmount(decimal)" />.
    /// </summary>
    private readonly struct NormalizedTag
    {
    }
}
