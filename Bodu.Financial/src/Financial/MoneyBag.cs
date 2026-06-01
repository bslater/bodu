// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBag.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Bodu.Financial.Serialization;

namespace Bodu.Financial;

/// <summary>
/// Aggregates monetary balances across multiple currencies. Immutable; every mutator returns a new instance.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MoneyBag" /> is the multi-currency aggregate used to model portfolios, ledger totals that span
/// currencies, FX positions, or any other context where amounts in different currencies must be tracked together but
/// not silently merged. Zero balances are pruned automatically on every operation.
/// </para>
/// <para>
/// Enumeration yields one <see cref="Money" /> per non-zero currency, in ISO-code lexicographic order so the
/// iteration is stable and reproducible across runs.
/// </para>
/// <para>
/// The type-level <see cref="JsonConverterAttribute" /> always serializes the canonical
/// <see cref="Serialization.FinancialJsonPolicy.Strict" /> object shape; the compact and lenient shapes are opt-in only
/// and must be selected through
/// <see cref="Serialization.FinancialJsonSerializerOptionsExtensions.AddFinancialJsonConverters(System.Text.Json.JsonSerializerOptions, Serialization.FinancialJsonPolicy)" />.
/// </para>
/// </remarks>
[DebuggerDisplay("{Count} currencies")]
[JsonConverter(typeof(MoneyBagJsonConverter))]
public sealed partial class MoneyBag :
    IEquatable<MoneyBag>,
    IEnumerable<Money>
{
    /// <summary>
    /// The shared empty bag instance.
    /// </summary>
    public static readonly MoneyBag Empty = new();

    /// <summary>
    /// The internal balance map keyed by ISO 4217 code (case-sensitive) and kept in ISO-code order.
    /// </summary>
    private readonly ImmutableSortedDictionary<string, decimal> _balances;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyBag" /> class.
    /// </summary>
    public MoneyBag()
    {
        _balances = ImmutableSortedDictionary.Create<string, decimal>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyBag" /> class from a sequence of <see cref="Money" />
    /// balances, summing amounts with the same ISO code.
    /// </summary>
    /// <param name="balances">The starting balances.</param>
    /// <exception cref="ArgumentNullException"><paramref name="balances" /> is <see langword="null" />.</exception>
    public MoneyBag(IEnumerable<Money> balances)
    {
        ThrowHelper.ThrowIfNull(balances);

        ImmutableSortedDictionary<string, decimal>.Builder builder =
            ImmutableSortedDictionary.CreateBuilder<string, decimal>(StringComparer.Ordinal);
        foreach (Money balance in balances)
        {
            var iso = balance.IsoCode;
            if (string.IsNullOrEmpty(iso))
                throw new ArgumentException(FinancialResourceStrings.Arg_Invalid_BalanceMissingIsoCode, nameof(balances));

            builder[iso] = builder.TryGetValue(iso, out var existing) ? existing + balance.Amount : balance.Amount;
        }

        // Prune zero balances introduced by mutual cancellation during the sum.
        foreach (var iso in builder.Where(entry => entry.Value == 0m).Select(entry => entry.Key).ToList())
            builder.Remove(iso);

        _balances = builder.ToImmutable();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyBag" /> class from an already-built immutable map (internal
    /// fast path used by factories and mutators).
    /// </summary>
    /// <param name="source">The balance map the new bag adopts.</param>
    private MoneyBag(ImmutableSortedDictionary<string, decimal> source)
    {
        _balances = source;
    }

    /// <summary>
    /// Gets a value indicating whether this bag carries no balances.
    /// </summary>
    /// <returns><see langword="true" /> when the bag is empty; otherwise <see langword="false" />.</returns>
    public bool IsEmpty =>
        _balances.Count == 0;

    /// <summary>
    /// Gets the number of distinct currencies carrying a non-zero balance.
    /// </summary>
    /// <returns>The number of currency slots.</returns>
    public int Count =>
        _balances.Count;

    /// <summary>
    /// Gets a read-only view of the balance map keyed by ISO code, in ISO-code order.
    /// </summary>
    /// <returns>
    /// The immutable balance map. Because the backing store is an
    /// <see cref="ImmutableSortedDictionary{TKey, TValue}" />, the view is genuinely read-only and is returned without
    /// allocating a wrapper.
    /// </returns>
    public IReadOnlyDictionary<string, decimal> Balances =>
        _balances;

    /// <summary>
    /// Enumerates the non-zero balances in ISO-code lexicographic order.
    /// </summary>
    /// <returns>An enumerator over <see cref="Money" /> entries.</returns>
    public IEnumerator<Money> GetEnumerator()
    {
        foreach (KeyValuePair<string, decimal> entry in _balances)
            yield return Money.FromNormalized(entry.Value, entry.Key);
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Returns a new bag with <paramref name="amount" /> added to the balance for its currency.
    /// </summary>
    /// <param name="amount">The amount to add.</param>
    /// <returns>The updated bag.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="amount" /> has no ISO code (default-initialised).
    /// </exception>
    public MoneyBag Add(Money amount)
    {
        var iso = amount.IsoCode;
        if (string.IsNullOrEmpty(iso))
            throw new ArgumentException(FinancialResourceStrings.Arg_Invalid_MoneyMissingIsoCode, nameof(amount));

        if (amount.IsZero)
            return this;

        if (_balances.TryGetValue(iso, out var existing))
        {
            var sum = existing + amount.Amount;
            return new MoneyBag(sum == 0m ? _balances.Remove(iso) : _balances.SetItem(iso, sum));
        }

        return new MoneyBag(_balances.SetItem(iso, amount.Amount));
    }

    /// <summary>
    /// Returns a new bag with the typed <paramref name="amount" /> added to the balance for its currency.
    /// </summary>
    /// <typeparam name="TCurrency">The currency type.</typeparam>
    /// <param name="amount">The amount to add.</param>
    /// <returns>The updated bag.</returns>
    public MoneyBag Add<TCurrency>(Money<TCurrency> amount)
        where TCurrency : ICurrency =>
        Add(amount.ToMoney());

    /// <summary>
    /// Returns a new bag with <paramref name="amount" /> subtracted from the balance for its currency.
    /// </summary>
    /// <param name="amount">The amount to subtract.</param>
    /// <returns>The updated bag.</returns>
    public MoneyBag Subtract(Money amount) =>
        Add(-amount);

    /// <summary>
    /// Returns a new bag with the typed <paramref name="amount" /> subtracted.
    /// </summary>
    /// <typeparam name="TCurrency">The currency type.</typeparam>
    /// <param name="amount">The amount to subtract.</param>
    /// <returns>The updated bag.</returns>
    public MoneyBag Subtract<TCurrency>(Money<TCurrency> amount)
        where TCurrency : ICurrency =>
        Subtract(amount.ToMoney());

    /// <summary>
    /// Returns a new bag containing the union of this bag's balances and <paramref name="other" />'s, summing
    /// per-currency amounts.
    /// </summary>
    /// <param name="other">The other bag to combine.</param>
    /// <returns>The combined bag.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public MoneyBag Combine(MoneyBag other)
    {
        ThrowHelper.ThrowIfNull(other);
        if (other.IsEmpty)
            return this;
        if (IsEmpty)
            return other;

        ImmutableSortedDictionary<string, decimal>.Builder builder = _balances.ToBuilder();
        foreach (KeyValuePair<string, decimal> entry in other._balances)
        {
            if (builder.TryGetValue(entry.Key, out var existing))
            {
                var sum = existing + entry.Value;
                if (sum == 0m)
                    builder.Remove(entry.Key);
                else
                    builder[entry.Key] = sum;
            }
            else
            {
                builder[entry.Key] = entry.Value;
            }
        }

        return new MoneyBag(builder.ToImmutable());
    }

    /// <summary>
    /// Returns the balance for the currency identified by <paramref name="isoCode" />, or <see langword="null" /> when
    /// the bag has no entry for that currency.
    /// </summary>
    /// <param name="isoCode">The ISO 4217 code.</param>
    /// <returns>The runtime-tagged balance, or <see langword="null" /> when absent.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="isoCode" /> is <see langword="null" />.</exception>
    public Money? GetBalance(string isoCode)
    {
        ThrowHelper.ThrowIfNull(isoCode);
        return _balances.TryGetValue(isoCode, out var amount)
            ? Money.FromNormalized(amount, isoCode)
            : null;
    }

    /// <summary>
    /// Returns the typed balance for <typeparamref name="TCurrency" />, or <see langword="null" /> when absent.
    /// </summary>
    /// <typeparam name="TCurrency">The currency type.</typeparam>
    /// <returns>The typed balance, or <see langword="null" /> when the bag has no entry for that currency.</returns>
    public Money<TCurrency>? GetBalance<TCurrency>()
        where TCurrency : ICurrency =>
        _balances.TryGetValue(CurrencyMetadata<TCurrency>.Value.IsoCode, out var amount)
            ? new Money<TCurrency>(amount)
            : null;

    /// <summary>
    /// Determines whether two bags carry the same balances.
    /// </summary>
    /// <param name="other">The other bag.</param>
    /// <returns><see langword="true" /> on equality.</returns>
    public bool Equals(MoneyBag? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_balances.Count != other._balances.Count) return false;

        foreach (KeyValuePair<string, decimal> entry in _balances)
        {
            if (!other._balances.TryGetValue(entry.Key, out var otherAmount))
                return false;
            if (entry.Value != otherAmount)
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        Equals(obj as MoneyBag);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        HashCode hash = default;
        foreach (KeyValuePair<string, decimal> entry in _balances)
        {
            hash.Add(entry.Key);
            hash.Add(entry.Value);
        }
        return hash.ToHashCode();
    }

    /// <summary>
    /// Combines two bags.
    /// </summary>
    /// <param name="left">The first bag.</param>
    /// <param name="right">The second bag.</param>
    /// <returns>The combined bag.</returns>
    public static MoneyBag operator +(MoneyBag left, MoneyBag right)
    {
        ThrowHelper.ThrowIfNull(left);
        return left.Combine(right);
    }

    /// <summary>
    /// Adds a single <see cref="Money" /> to a bag.
    /// </summary>
    /// <param name="left">The bag.</param>
    /// <param name="right">The amount to add.</param>
    /// <returns>The updated bag.</returns>
    public static MoneyBag operator +(MoneyBag left, Money right)
    {
        ThrowHelper.ThrowIfNull(left);
        return left.Add(right);
    }

    /// <summary>
    /// Subtracts a <see cref="Money" /> from a bag.
    /// </summary>
    /// <param name="left">The bag.</param>
    /// <param name="right">The amount to subtract.</param>
    /// <returns>The updated bag.</returns>
    public static MoneyBag operator -(MoneyBag left, Money right)
    {
        ThrowHelper.ThrowIfNull(left);
        return left.Subtract(right);
    }

    /// <summary>
    /// Determines whether two bags are equal.
    /// </summary>
    /// <param name="left">The first bag.</param>
    /// <param name="right">The second bag.</param>
    /// <returns><see langword="true" /> when equal.</returns>
    public static bool operator ==(MoneyBag? left, MoneyBag? right)
    {
        return left is null ? right is null : left.Equals(right);
    }

    /// <summary>
    /// Determines whether two bags differ.
    /// </summary>
    /// <param name="left">The first bag.</param>
    /// <param name="right">The second bag.</param>
    /// <returns><see langword="true" /> when they differ.</returns>
    public static bool operator !=(MoneyBag? left, MoneyBag? right)
    {
        return !(left == right);
    }
}
