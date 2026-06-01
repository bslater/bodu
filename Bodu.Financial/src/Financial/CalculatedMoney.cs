// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoney.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

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
/// The currency is identified at runtime by ISO 4217 code, validated only for shape. Unlike <see cref="Money" />, the
/// currency need not be registered to participate in a calculation; registration (or an explicit scale) is required
/// only when materialising a settlement value.
/// </para>
/// </remarks>
[DebuggerDisplay("{Amount} {IsoCode,nq} (unrounded)")]
public readonly partial struct CalculatedMoney
{
    /// <summary>
    /// The unrounded amount in the major unit of the currency identified by <see cref="_isoCode" />.
    /// </summary>
    private readonly decimal _amount;

    /// <summary>
    /// The ISO 4217 alphabetic code identifying the currency, or <see langword="null" /> for a default-initialised
    /// value.
    /// </summary>
    private readonly string? _isoCode;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculatedMoney" /> struct from an amount and ISO 4217 code,
    /// preserving the amount's full precision.
    /// </summary>
    /// <param name="amount">The unrounded monetary amount in the major unit.</param>
    /// <param name="isoCode">The ISO 4217 three-letter alphabetic code identifying the currency.</param>
    /// <exception cref="ArgumentNullException"><paramref name="isoCode" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="isoCode" /> is not three uppercase ASCII letters.</exception>
    public CalculatedMoney(decimal amount, string isoCode)
    {
        FinancialThrowHelper.ThrowIfNotValidIsoCode(isoCode);

        _amount = amount;
        _isoCode = isoCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculatedMoney" /> struct from pre-validated field values.
    /// </summary>
    /// <param name="amount">The unrounded amount.</param>
    /// <param name="isoCode">The ISO 4217 code, or <see langword="null" /> for a currency-less value.</param>
    /// <param name="_">Discriminator that selects the pre-validated construction path.</param>
    private CalculatedMoney(decimal amount, string? isoCode, bool _)
    {
        _amount = amount;
        _isoCode = isoCode;
    }

    /// <summary>
    /// Returns a copy of this value with a different amount, preserving the ISO code.
    /// </summary>
    /// <param name="amount">The replacement unrounded amount.</param>
    /// <returns>The updated <see cref="CalculatedMoney" />.</returns>
    private CalculatedMoney WithAmount(decimal amount) =>
        new(amount, _isoCode, false);
}
