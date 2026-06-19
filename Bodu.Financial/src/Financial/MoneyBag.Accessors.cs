// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBag.Accessors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public sealed partial class MoneyBag
{
    /// <summary>
    /// Creates a <see cref="MoneyBag" /> from the supplied balances, summing amounts with the same ISO code.
    /// </summary>
    /// <param name="balances">The starting balances.</param>
    /// <returns>The constructed bag.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="balances" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">A balance carries no ISO code (default-initialised).</exception>
    public static MoneyBag Of(params Money[] balances)
    {
        ThrowHelper.ThrowIfNull(balances);
        return new MoneyBag(balances);
    }

    /// <summary>
    /// Creates a <see cref="MoneyBag" /> from a sequence of balances, summing amounts with the same ISO code.
    /// </summary>
    /// <param name="balances">The starting balances.</param>
    /// <returns>The constructed bag.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="balances" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">A balance carries no ISO code (default-initialised).</exception>
    public static MoneyBag FromBalances(IEnumerable<Money> balances) =>
        new(balances);

    /// <summary>
    /// Attempts to retrieve the balance for the currency identified by <paramref name="code" />.
    /// </summary>
    /// <param name="code">The currency identifying the balance.</param>
    /// <param name="balance">When this method returns <see langword="true" />, the runtime-tagged balance.</param>
    /// <returns>
    /// <see langword="true" /> when the bag has an entry for the currency; otherwise <see langword="false" />.
    /// </returns>
    public bool TryGetBalance(CurrencyCode code, out Money balance)
    {
        if (code != CurrencyCode.None && _balances.TryGetValue(code, out decimal amount))
        {
            balance = Money.FromNormalized(amount, code);
            return true;
        }

        balance = default;
        return false;
    }

    /// <summary>
    /// Returns the balance for <paramref name="currency" />, or <see langword="null" /> when the bag has no entry for
    /// that currency.
    /// </summary>
    /// <param name="currency">The currency metadata identifying the balance.</param>
    /// <returns>The balance, or <see langword="null" /> when absent.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="currency" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="currency" />'s ISO code does not correspond to any <see cref="CurrencyCode" /> member.
    /// </exception>
    public Money? GetBalance(CurrencyInfo currency)
    {
        ThrowHelper.ThrowIfNull(currency);
        return GetBalance(currency.ToCurrencyCode());
    }

    /// <summary>
    /// Returns the balance for the currency identified by <paramref name="code" />, or <see langword="null" /> when the
    /// bag has no entry for that currency.
    /// </summary>
    /// <param name="code">The currency identifying the balance.</param>
    /// <returns>The balance, or <see langword="null" /> when absent.</returns>
    public Money? GetBalance(CurrencyCode code) =>
        code != CurrencyCode.None && _balances.TryGetValue(code, out decimal amount)
            ? Money.FromNormalized(amount, code)
            : null;
}
