// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBag.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;
using Bodu.Financial.Currencies;

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
/// Enumeration yields one <see cref="Money" /> per non-zero currency, in ISO-code lexicographic order so the iteration
/// is stable and reproducible across runs.
/// </para>
/// <para>
/// JSON serialization ships in the companion <c>Bodu.Financial.Serialization.Json</c> package; the type carries no
/// <c>[JsonConverter]</c> attribute, so register the financial converters on the target <c>JsonSerializerOptions</c>
/// via its <c>AddFinancialJsonConverters</c> extension.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Financial;
/// using Bodu.Financial.Currencies;
///
/// // Every mutator returns a new bag; same-currency amounts are summed automatically.
/// MoneyBag portfolio = MoneyBag.Empty
///     .Add(new Money(1_000m, CurrencyCode.USD))
///     .Add(new Money(250m, CurrencyCode.USD))      // folds into the USD slot -> 1,250.00 USD
///     .Add<EUR>(new Money<EUR>(500m));             // typed overload
///
/// // Merge two bags, summing per currency.
/// MoneyBag combined = portfolio.Combine(new MoneyBag([new Money(200m, CurrencyCode.GBP)]));
///
/// // Read a single currency back, typed and null-safe.
/// Money<USD>? usd = combined.GetBalance<USD>();    // 1,250.00 USD
/// int currencies = combined.Count;                 // 3 (USD, EUR, GBP)
///]]>
/// </code>
/// </example>
/// </remarks>
[DebuggerDisplay("{Count} currencies")]
public sealed partial class MoneyBag
    : IEquatable<MoneyBag>
    , IEnumerable<Money>
{
    /// <summary>Orders <see cref="CurrencyCode" /> keys by their ISO 4217 alphabetic code (ordinal) rather than their numeric enum value, so enumeration stays in ISO-code lexicographic order as it was when the bag was keyed by string.</summary>
    /// <remarks>
    /// Declared before <see cref="Empty" /> so the shared empty instance captures this comparer during static
    /// initialization rather than the default (numeric) ordering — static fields initialize in textual order.
    /// </remarks>
    private static readonly IComparer<CurrencyCode> s_codeComparer =
        Comparer<CurrencyCode>.Create(static (a, b) => string.CompareOrdinal(a.ToString(), b.ToString()));

    /// <summary>The shared empty bag instance.</summary>
    public static readonly MoneyBag Empty = new();

    /// <summary>The internal balance map keyed by <see cref="CurrencyCode" /> and kept in ISO-code lexicographic order.</summary>
    private readonly ImmutableSortedDictionary<CurrencyCode, decimal> _balances;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyBag" /> class.
    /// </summary>
    public MoneyBag()
    {
        _balances = ImmutableSortedDictionary.Create<CurrencyCode, decimal>(s_codeComparer);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyBag" /> class from a sequence of <see cref="Money" />
    /// balances, summing amounts with the same currency.
    /// </summary>
    /// <param name="balances">The starting balances.</param>
    /// <exception cref="ArgumentNullException"><paramref name="balances" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">A balance carries no currency (default-initialised).</exception>
    public MoneyBag(IEnumerable<Money> balances)
    {
        ThrowHelper.ThrowIfNull(balances);

        ImmutableSortedDictionary<CurrencyCode, decimal>.Builder builder =
            ImmutableSortedDictionary.CreateBuilder<CurrencyCode, decimal>(s_codeComparer);
        foreach (Money balance in balances)
        {
            CurrencyCode code = balance.Code;
            if (code == CurrencyCode.None)
                throw new ArgumentException(FinancialResourceStrings.Arg_Invalid_BalanceMissingIsoCode, nameof(balances));

            decimal value = NormalizeToRegistry(balance);
            builder[code] = builder.TryGetValue(code, out decimal existing) ? existing + value : value;
        }

        // Prune zero balances introduced by mutual cancellation during the sum.
        foreach (CurrencyCode code in builder.Where(entry => entry.Value == 0m).Select(entry => entry.Key).ToList())
            builder.Remove(code);

        _balances = builder.ToImmutable();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyBag" /> class from an already-built immutable map (internal
    /// fast path used by factories and mutators).
    /// </summary>
    /// <param name="source">The balance map the new bag adopts.</param>
    private MoneyBag(ImmutableSortedDictionary<CurrencyCode, decimal> source)
    {
        _balances = source;
    }

    /// <summary>
    /// Gets a value indicating whether this bag carries no balances.
    /// </summary>
    /// <value><see langword="true" /> when the bag is empty; otherwise <see langword="false" />.</value>
    public bool IsEmpty =>
        _balances.Count == 0;

    /// <summary>
    /// Gets the number of distinct currencies carrying a non-zero balance.
    /// </summary>
    /// <value>The number of currency slots.</value>
    public int Count =>
        _balances.Count;

    /// <summary>
    /// Gets a read-only view of the balance map keyed by <see cref="CurrencyCode" />, in ISO-code lexicographic order.
    /// </summary>
    /// <value>
    /// The immutable balance map. Because the backing store is an
    /// <see cref="ImmutableSortedDictionary{TKey, TValue}" />, the view is genuinely read-only and is returned without
    /// allocating a wrapper.
    /// </value>
    public IReadOnlyDictionary<CurrencyCode, decimal> Balances =>
        _balances;

    /// <summary>
    /// Enumerates the non-zero balances in ISO-code lexicographic order.
    /// </summary>
    /// <returns>An enumerator over <see cref="Money" /> entries.</returns>
    public IEnumerator<Money> GetEnumerator()
    {
        foreach (KeyValuePair<CurrencyCode, decimal> entry in _balances)
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
    /// <paramref name="amount" /> has no currency (default-initialised).
    /// </exception>
    /// <remarks>
    /// The bag is a settlement-precision container: the incoming amount is rounded to its currency's registered
    /// minor units (banker's rounding) before it is folded into the balance, so an explicit-scale unit price settles
    /// on entry. Settle high-precision amounts deliberately — via
    /// <see cref="CalculatedMoney.RoundToMoney(MonetaryContext?)" /> — when a different rounding rule is required.
    /// </remarks>
    public MoneyBag Add(Money amount)
    {
        CurrencyCode code = amount.Code;
        if (code == CurrencyCode.None)
            throw new ArgumentException(FinancialResourceStrings.Arg_Invalid_MoneyMissingIsoCode, nameof(amount));

        decimal value = NormalizeToRegistry(amount);
        if (value == 0m)
            return this;

        if (_balances.TryGetValue(code, out decimal existing))
        {
            decimal sum = existing + value;
            return new MoneyBag(sum == 0m ? _balances.Remove(code) : _balances.SetItem(code, sum));
        }

        return new MoneyBag(_balances.SetItem(code, value));
    }

    /// <summary>
    /// Normalises an incoming amount to its currency's registered minor-unit precision.
    /// </summary>
    /// <param name="amount">The amount entering the bag.</param>
    /// <returns>The amount rounded to the registered minor units using banker's rounding.</returns>
    /// <remarks>
    /// Balances are held at the currency's registered minor units because the JSON wire form carries no per-balance
    /// scale — rounding on entry keeps the in-memory balances identical to what a serialization round-trip restores,
    /// instead of letting sub-minor-unit digits mutate silently on the first save/load.
    /// </remarks>
    private static decimal NormalizeToRegistry(Money amount) =>
        MoneyMath.Round(amount.Amount, CurrencyInfo.FromCurrencyCode(amount.Code).MinorUnits, MidpointRounding.ToEven);

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

        var builder = _balances.ToBuilder();
        foreach (KeyValuePair<CurrencyCode, decimal> entry in other._balances)
        {
            if (builder.TryGetValue(entry.Key, out decimal existing))
            {
                decimal sum = existing + entry.Value;
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
    /// Returns the typed balance for <typeparamref name="TCurrency" />, or <see langword="null" /> when absent.
    /// </summary>
    /// <typeparam name="TCurrency">The currency type.</typeparam>
    /// <returns>The typed balance, or <see langword="null" /> when the bag has no entry for that currency.</returns>
    public Money<TCurrency>? GetBalance<TCurrency>()
        where TCurrency : ICurrency =>
        _balances.TryGetValue(CurrencyMetadata<TCurrency>.Value.Code, out decimal amount)
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

        foreach (KeyValuePair<CurrencyCode, decimal> entry in _balances)
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
        foreach (KeyValuePair<CurrencyCode, decimal> entry in _balances)
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
