// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyValue.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Bodu.Financial;

/// <summary>
/// Represents an immutable monetary amount whose currency is identified at runtime by ISO 4217 code, in contrast to
/// <see cref="Money{TCurrency}" /> where the currency is a type parameter.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MoneyValue" /> is the runtime-tagged counterpart of <see cref="Money{TCurrency}" />. Use it when the
/// currency is data rather than part of the type — for example, when deserialising payloads that carry the currency
/// code, or when modelling a generic invoicing engine that processes arbitrary currencies. The trade-off is that
/// cross-currency arithmetic and comparison surface as <see cref="InvalidOperationException" /> at runtime instead of
/// as compile errors.
/// </para>
/// <para>
/// The amount is rounded on construction to the minor-unit precision reported by <see cref="CurrencyRegistry" /> for
/// the supplied ISO code, using banker's rounding by default. When the ISO code is not in the registry the amount is
/// stored at its source precision; consumers can call <see cref="CurrencyRegistry.Register" /> to register custom
/// currencies ahead of construction.
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
    /// The ISO 4217 alphabetic code identifying the currency, or <see langword="null" /> for a default-initialised
    /// value.
    /// </summary>
    private readonly string? _isoCode;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyValue" /> struct from an amount and ISO 4217 code, rounding
    /// the amount to the currency's minor-unit precision using banker's rounding.
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
    /// Initializes a new instance of the <see cref="MoneyValue" /> struct from an amount and ISO 4217 code, rounding
    /// the amount to the currency's minor-unit precision using the supplied rule.
    /// </summary>
    /// <param name="amount">The monetary amount in the major unit.</param>
    /// <param name="isoCode">The ISO 4217 three-letter alphabetic code identifying the currency.</param>
    /// <param name="rounding">The midpoint-rounding rule applied when normalising to the minor-unit precision.</param>
    /// <exception cref="ArgumentNullException"><paramref name="isoCode" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="isoCode" /> is not exactly three uppercase ASCII letters — empty, whitespace, the wrong length,
    /// lowercase, or contains non-letter characters.
    /// </exception>
    /// <remarks>
    /// Currency integrity matches <see cref="Money{TCurrency}" />: the ISO code is validated to ISO 4217's
    /// three-uppercase-ASCII-letters shape regardless of whether the code is in <see cref="CurrencyRegistry" />. For
    /// codes that are registered, the amount is rounded to the registry's <c>MinorUnits</c>; for codes that are valid
    /// in shape but not registered, the amount is stored at its source precision so consumer code that handles custom
    /// or test currencies still works.
    /// </remarks>
    public MoneyValue(decimal amount, string isoCode, MidpointRounding rounding)
    {
        ThrowHelper.ThrowIfNull(isoCode);
        ValidateIsoCode(isoCode);

        _isoCode = isoCode;
        _amount = CurrencyRegistry.TryGet(isoCode, out CurrencyInfo? info) && info is not null
            ? decimal.Round(amount, info.MinorUnits, rounding)
            : amount;
    }

    /// <summary>
    /// Validates that <paramref name="isoCode" /> is exactly three uppercase ASCII letters.
    /// </summary>
    /// <param name="isoCode">The candidate ISO code.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="isoCode" /> is not exactly three uppercase ASCII letters.
    /// </exception>
    private static void ValidateIsoCode(string isoCode)
    {
        if (isoCode.Length != 3)
        {
            throw new ArgumentException(
                $"ISO 4217 code must be exactly three letters, but '{isoCode}' has {isoCode.Length}.",
                nameof(isoCode));
        }

        for (var i = 0; i < 3; i++)
        {
            var c = isoCode[i];
            if (c is < 'A' or > 'Z')
            {
                throw new ArgumentException(
                    $"ISO 4217 code must be three uppercase ASCII letters, but '{isoCode}' contains '{c}'.",
                    nameof(isoCode));
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MoneyValue" /> struct from an already-normalised amount and ISO
    /// code, bypassing rounding.
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
    /// Creates a <see cref="MoneyValue" /> from an amount and ISO code already at the currency's minor-unit precision.
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
