// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyDtoAdapter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.Serialization;

/// <summary>
/// Converts between <see cref="Money" /> and the Google-compatible <see cref="MoneyDto" /> transport shape.
/// </summary>
public static class MoneyDtoAdapter
{
    /// <summary>
    /// The number of nano-units in one major unit.
    /// </summary>
    private const decimal NanosPerUnit = 1_000_000_000m;

    /// <summary>
    /// The inclusive bound for the magnitude of the nano-unit fraction.
    /// </summary>
    private const int MaxNanos = 999_999_999;

    /// <summary>
    /// Converts a <see cref="Money" /> to its <see cref="MoneyDto" /> transport representation.
    /// </summary>
    /// <param name="money">The amount to convert.</param>
    /// <returns>The transport-shaped representation.</returns>
    public static MoneyDto ToDto(Money money)
    {
        var amount = money.Amount;
        var units = decimal.ToInt64(decimal.Truncate(amount));
        var nanos = (int)decimal.Round((amount - units) * NanosPerUnit, 0, MidpointRounding.ToEven);

        return new MoneyDto(money.IsoCode, units, nanos);
    }

    /// <summary>
    /// Converts a <see cref="MoneyDto" /> to a <see cref="Money" />, applying <paramref name="unknownCurrency" /> when
    /// the currency is unregistered.
    /// </summary>
    /// <param name="dto">The transport-shaped representation.</param>
    /// <param name="unknownCurrency">The policy applied when the currency is unregistered.</param>
    /// <returns>The reconstructed <see cref="Money" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dto" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// The nanos value is out of range, the units and nanos signs disagree, or the currency code is invalid or
    /// unregistered under the policy.
    /// </exception>
    public static Money ToMoney(MoneyDto dto, UnknownCurrencyPolicy unknownCurrency = UnknownCurrencyPolicy.Reject)
    {
        ThrowHelper.ThrowIfNull(dto);

        if (dto.Nanos < -MaxNanos || dto.Nanos > MaxNanos)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Arg_Invalid_MoneyDtoNanosRange, dto.Nanos),
                nameof(dto));
        }

        if (dto.Units != 0 && dto.Nanos != 0 && Math.Sign(dto.Units) != Math.Sign(dto.Nanos))
        {
            throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Arg_Invalid_MoneyDtoSignMismatch, dto.Units, dto.Nanos),
                nameof(dto));
        }

        var amount = dto.Units + (dto.Nanos / NanosPerUnit);
        return Money.From(amount, dto.CurrencyCode, unknownCurrency);
    }
}
