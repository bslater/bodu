// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBag.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;

namespace Bodu.Numerics;

/// <summary>
/// Aggregates monetary balances across multiple currencies. Immutable; every mutator returns a new instance.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MoneyBag" /> is the multi-currency aggregate used to model portfolios, ledger totals that span
/// currencies, FX positions, or any other context where amounts in different currencies must be tracked
/// together but not silently merged. Zero balances are pruned automatically on every operation.
/// </para>
/// <para>
/// Enumeration yields one <see cref="MoneyValue" /> per non-zero currency, in ISO-code lexicographic order so
/// the iteration is stable and reproducible across runs.
/// </para>
/// </remarks>
[DebuggerDisplay("{Count} currencies")]
[JsonConverter(typeof(MoneyBagJsonConverter))]
public sealed partial class MoneyBag :
    IEquatable<MoneyBag>,
    IEnumerable<MoneyValue>
{
    /// <summary>
    /// The shared empty bag instance.
    /// </summary>
    public static readonly MoneyBag Empty = new();

    /// <summary>
    /// The internal balance dictionary keyed by ISO 4217 code (case-sensitive).
    /// </summary>
    private readonly Dictionary<string, decimal> _balances;

    /// <summary>
    /// Initializes an empty <see cref="MoneyBag" />.
    /// </summary>
    public MoneyBag()
    {
        _balances = new Dictionary<string, decimal>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Initializes a <see cref="MoneyBag" /> from a sequence of <see cref="MoneyValue" /> balances, summing
    /// amounts with the same ISO code.
    /// </summary>
    /// <param name="balances">The starting balances.</param>
    /// <exception cref="ArgumentNullException"><paramref name="balances" /> is <see langword="null" />.</exception>
    public MoneyBag(IEnumerable<MoneyValue> balances)
    {
        ThrowHelper.ThrowIfNull(balances);

        _balances = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (MoneyValue balance in balances)
        {
            string iso = balance.IsoCode;
            if (string.IsNullOrEmpty(iso))
                throw new ArgumentException("Every balance must carry a non-empty ISO code; default(MoneyValue) is not a valid balance.", nameof(balances));

            if (_balances.TryGetValue(iso, out decimal existing))
                _balances[iso] = existing + balance.Amount;
            else
                _balances[iso] = balance.Amount;
        }

        // Prune zero balances introduced by mutual cancellation during the sum.
        List<string>? toRemove = null;
        foreach (KeyValuePair<string, decimal> entry in _balances)
        {
            if (entry.Value == 0m)
            {
                toRemove ??= new List<string>();
                toRemove.Add(entry.Key);
            }
        }

        if (toRemove is not null)
        {
            foreach (string iso in toRemove)
                _balances.Remove(iso);
        }
    }

    /// <summary>
    /// Initializes a bag from an already-built dictionary (internal fast path used by mutators).
    /// </summary>
    /// <param name="source">The balance dictionary the new bag takes ownership of.</param>
    private MoneyBag(Dictionary<string, decimal> source)
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
    /// Gets a read-only snapshot of the balance dictionary keyed by ISO code.
    /// </summary>
    /// <returns>
    /// A genuinely read-only view of the balances. The returned dictionary is a
    /// <see cref="System.Collections.ObjectModel.ReadOnlyDictionary{TKey, TValue}" /> wrapper around the
    /// internal storage, so a consumer cannot bypass immutability by casting the result back to
    /// <see cref="Dictionary{TKey, TValue}" /> and mutating it.
    /// </returns>
    public IReadOnlyDictionary<string, decimal> Balances =>
        new System.Collections.ObjectModel.ReadOnlyDictionary<string, decimal>(_balances);

    /// <summary>
    /// Enumerates the non-zero balances in ISO-code lexicographic order.
    /// </summary>
    /// <returns>An enumerator over <see cref="MoneyValue" /> entries.</returns>
    public IEnumerator<MoneyValue> GetEnumerator()
    {
        foreach (KeyValuePair<string, decimal> entry in _balances.OrderBy(p => p.Key, StringComparer.Ordinal))
            yield return MoneyValue.FromNormalized(entry.Value, entry.Key);
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Returns a new bag with <paramref name="amount" /> added to the balance for its currency.
    /// </summary>
    /// <param name="amount">The amount to add.</param>
    /// <returns>The updated bag.</returns>
    /// <exception cref="ArgumentException"><paramref name="amount" /> has no ISO code (default-initialised).</exception>
    public MoneyBag Add(MoneyValue amount)
    {
        string iso = amount.IsoCode;
        if (string.IsNullOrEmpty(iso))
            throw new ArgumentException("MoneyValue must carry a non-empty ISO code.", nameof(amount));

        if (amount.IsZero)
            return this;

        Dictionary<string, decimal> copy = new(_balances, StringComparer.Ordinal);
        if (copy.TryGetValue(iso, out decimal existing))
        {
            decimal sum = existing + amount.Amount;
            if (sum == 0m)
                copy.Remove(iso);
            else
                copy[iso] = sum;
        }
        else
        {
            copy[iso] = amount.Amount;
        }

        return new MoneyBag(copy);
    }

    /// <summary>
    /// Returns a new bag with the typed <paramref name="amount" /> added to the balance for its currency.
    /// </summary>
    /// <typeparam name="TCurrency">The currency type.</typeparam>
    /// <param name="amount">The amount to add.</param>
    /// <returns>The updated bag.</returns>
    public MoneyBag Add<TCurrency>(Money<TCurrency> amount)
        where TCurrency : ICurrency =>
        Add(MoneyValue.FromTyped(amount));

    /// <summary>
    /// Returns a new bag with <paramref name="amount" /> subtracted from the balance for its currency.
    /// </summary>
    /// <param name="amount">The amount to subtract.</param>
    /// <returns>The updated bag.</returns>
    public MoneyBag Subtract(MoneyValue amount) =>
        Add(-amount);

    /// <summary>
    /// Returns a new bag with the typed <paramref name="amount" /> subtracted.
    /// </summary>
    /// <typeparam name="TCurrency">The currency type.</typeparam>
    /// <param name="amount">The amount to subtract.</param>
    /// <returns>The updated bag.</returns>
    public MoneyBag Subtract<TCurrency>(Money<TCurrency> amount)
        where TCurrency : ICurrency =>
        Subtract(MoneyValue.FromTyped(amount));

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

        Dictionary<string, decimal> copy = new(_balances, StringComparer.Ordinal);
        foreach (KeyValuePair<string, decimal> entry in other._balances)
        {
            if (copy.TryGetValue(entry.Key, out decimal existing))
            {
                decimal sum = existing + entry.Value;
                if (sum == 0m)
                    copy.Remove(entry.Key);
                else
                    copy[entry.Key] = sum;
            }
            else
            {
                copy[entry.Key] = entry.Value;
            }
        }

        return new MoneyBag(copy);
    }

    /// <summary>
    /// Returns the balance for the currency identified by <paramref name="isoCode" />, or <see langword="null" />
    /// when the bag has no entry for that currency.
    /// </summary>
    /// <param name="isoCode">The ISO 4217 code.</param>
    /// <returns>The runtime-tagged balance, or <see langword="null" /> when absent.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="isoCode" /> is <see langword="null" />.</exception>
    public MoneyValue? GetBalance(string isoCode)
    {
        ThrowHelper.ThrowIfNull(isoCode);
        return _balances.TryGetValue(isoCode, out decimal amount)
            ? MoneyValue.FromNormalized(amount, isoCode)
            : null;
    }

    /// <summary>
    /// Returns the typed balance for <typeparamref name="TCurrency" />, or <see langword="null" /> when absent.
    /// </summary>
    /// <typeparam name="TCurrency">The currency type.</typeparam>
    /// <returns>The typed balance, or <see langword="null" /> when the bag has no entry for that currency.</returns>
    public Money<TCurrency>? GetBalance<TCurrency>()
        where TCurrency : ICurrency =>
        _balances.TryGetValue(CurrencyMetadata<TCurrency>.Value.IsoCode, out decimal amount)
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
            if (!other._balances.TryGetValue(entry.Key, out decimal otherAmount))
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
        foreach (KeyValuePair<string, decimal> entry in _balances.OrderBy(p => p.Key, StringComparer.Ordinal))
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
    /// Adds a single <see cref="MoneyValue" /> to a bag.
    /// </summary>
    /// <param name="left">The bag.</param>
    /// <param name="right">The amount to add.</param>
    /// <returns>The updated bag.</returns>
    public static MoneyBag operator +(MoneyBag left, MoneyValue right)
    {
        ThrowHelper.ThrowIfNull(left);
        return left.Add(right);
    }

    /// <summary>
    /// Subtracts a <see cref="MoneyValue" /> from a bag.
    /// </summary>
    /// <param name="left">The bag.</param>
    /// <param name="right">The amount to subtract.</param>
    /// <returns>The updated bag.</returns>
    public static MoneyBag operator -(MoneyBag left, MoneyValue right)
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
    public static bool operator ==(MoneyBag? left, MoneyBag? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>
    /// Determines whether two bags differ.
    /// </summary>
    /// <param name="left">The first bag.</param>
    /// <param name="right">The second bag.</param>
    /// <returns><see langword="true" /> when they differ.</returns>
    public static bool operator !=(MoneyBag? left, MoneyBag? right) =>
        !(left == right);
}
