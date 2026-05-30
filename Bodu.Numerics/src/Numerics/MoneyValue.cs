// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyValue.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Bodu.Numerics;

/// <summary>
/// Represents an immutable monetary amount whose currency is identified at runtime by ISO 4217 code, in
/// contrast to <see cref="Money{TCurrency}" /> where the currency is a type parameter.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MoneyValue" /> is the runtime-tagged counterpart of <see cref="Money{TCurrency}" />. Use it when
/// the currency is data rather than part of the type — for example, when deserialising payloads that carry the
/// currency code, or when modelling a generic invoicing engine that processes arbitrary currencies. The
/// trade-off is that cross-currency arithmetic and comparison surface as
/// <see cref="InvalidOperationException" /> at runtime instead of as compile errors.
/// </para>
/// <para>
/// The amount is rounded on construction to the minor-unit precision reported by <see cref="CurrencyRegistry" />
/// for the supplied ISO code, using banker's rounding by default. When the ISO code is not in the registry the
/// amount is stored at its source precision; consumers can call <see cref="CurrencyRegistry.Register" /> to
/// register custom currencies ahead of construction.
/// </para>
/// </remarks>
[Serializable]
[DebuggerDisplay("{ToString(),nq}")]
[JsonConverter(typeof(MoneyValueJsonConverter))]
public readonly partial struct MoneyValue
{
    /// <summary>
    /// The rounded amount in the major unit of the currency identified by <see cref="_isoCode" />.
    /// </summary>
    private readonly decimal _amount;

    /// <summary>
    /// The ISO 4217 alphabetic code identifying the currency, or <see langword="null" /> for a default-initialised value.
    /// </summary>
    private readonly string? _isoCode;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyValue" /> struct from an amount and ISO 4217 code,
    /// rounding the amount to the currency's minor-unit precision using banker's rounding.
    /// </summary>
    /// <param name="amount">The monetary amount in the major unit.</param>
    /// <param name="isoCode">The ISO 4217 three-letter alphabetic code identifying the currency.</param>
    /// <exception cref="ArgumentNullException"><paramref name="isoCode" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="isoCode" /> is empty or whitespace.</exception>
    public MoneyValue(decimal amount, string isoCode)
        : this(amount, isoCode, MidpointRounding.ToEven)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyValue" /> struct from an amount and ISO 4217 code,
    /// rounding the amount to the currency's minor-unit precision using the supplied rule.
    /// </summary>
    /// <param name="amount">The monetary amount in the major unit.</param>
    /// <param name="isoCode">The ISO 4217 three-letter alphabetic code identifying the currency.</param>
    /// <param name="rounding">The midpoint-rounding rule applied when normalising to the minor-unit precision.</param>
    /// <exception cref="ArgumentNullException"><paramref name="isoCode" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="isoCode" /> is empty or whitespace.</exception>
    public MoneyValue(decimal amount, string isoCode, MidpointRounding rounding)
    {
        ThrowHelper.ThrowIfNull(isoCode);
        if (string.IsNullOrWhiteSpace(isoCode)) throw new ArgumentException("ISO code must not be empty.", nameof(isoCode));

        _isoCode = isoCode;
        if (CurrencyRegistry.TryGet(isoCode, out CurrencyInfo? info) && info is not null)
        {
            _amount = decimal.Round(amount, info.MinorUnits, rounding);
        }
        else
        {
            _amount = amount;
        }
    }

    /// <summary>
    /// Initializes a new instance from an already-normalised amount and ISO code, bypassing rounding.
    /// </summary>
    /// <param name="amount">The pre-normalised amount.</param>
    /// <param name="isoCode">The ISO 4217 code.</param>
    /// <param name="_">Discriminator that selects the no-normalisation path.</param>
    private MoneyValue(decimal amount, string isoCode, NormalizedTag _)
    {
        _amount = amount;
        _isoCode = isoCode;
    }

    /// <summary>
    /// Creates a <see cref="MoneyValue" /> from an amount and ISO code already at the currency's minor-unit
    /// precision.
    /// </summary>
    /// <param name="amount">The normalised amount.</param>
    /// <param name="isoCode">The ISO 4217 code.</param>
    /// <returns>The wrapped <see cref="MoneyValue" />.</returns>
    internal static MoneyValue FromNormalized(decimal amount, string isoCode) =>
        new(amount, isoCode, default(NormalizedTag));

    /// <summary>
    /// Private discriminator selecting the no-normalisation private constructor.
    /// </summary>
    private readonly struct NormalizedTag
    {
    }
}
