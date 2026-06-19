// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonetaryContext.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial;

/// <summary>
/// Carries the rounding and scaling policy applied at a monetary operation boundary — multiplication, division,
/// conversion, allocation, and the conversion of a high-precision calculation back to a settlement value.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="MonetaryContext" /> is supplied to operations rather than embedded in a <see cref="Money" /> value, so
/// the same amount can participate in calculations under different policies without carrying mutable state. Operations
/// that accept a nullable context treat <see langword="null" /> as <see cref="Default" />, preserving the historical
/// banker's-rounding behaviour.
/// </para>
/// </remarks>
public sealed record MonetaryContext
{
    /// <summary>
    /// Gets the shared default context: banker's rounding to the currency's minor units, no cash rounding,
    /// largest-remainder allocation, and rounding conversions at the target scale.
    /// </summary>
    /// <returns>The default <see cref="MonetaryContext" />.</returns>
    public static MonetaryContext Default { get; } = new();

    /// <summary>
    /// Gets the rounding strategy applied when reducing a computed amount to the resolved scale.
    /// </summary>
    /// <value>The rounding strategy; never <see langword="null" />.</value>
    /// <returns>The configured <see cref="IRoundingStrategy" />.</returns>
    public IRoundingStrategy Rounding { get; init; } = MidpointRoundingStrategy.ToEven;

    /// <summary>
    /// Gets the policy that determines the fractional-digit scale results are rounded to.
    /// </summary>
    /// <returns>The configured <see cref="ScalePolicy" />.</returns>
    public ScalePolicy ScalePolicy { get; init; } = ScalePolicy.CurrencyMinorUnits;

    /// <summary>
    /// Gets the explicit scale used when <see cref="ScalePolicy" /> is <see cref="ScalePolicy.Custom" />.
    /// </summary>
    /// <returns>The custom scale, or <see langword="null" /> when not applicable.</returns>
    public int? CustomScale { get; init; }

    /// <summary>
    /// Gets the cash-rounding policy applied to results.
    /// </summary>
    /// <returns>The configured <see cref="CashRoundingPolicy" />.</returns>
    public CashRoundingPolicy CashRounding { get; init; } = CashRoundingPolicy.None;

    /// <summary>
    /// Gets the allocation policy used when distributing an amount across shares.
    /// </summary>
    /// <returns>The configured <see cref="AllocationPolicy" />.</returns>
    public AllocationPolicy Allocation { get; init; } = AllocationPolicy.LargestRemainder;

    /// <summary>
    /// Gets the policy that determines when a currency conversion is rounded.
    /// </summary>
    /// <returns>The configured <see cref="ConversionRoundingPolicy" />.</returns>
    public ConversionRoundingPolicy ConversionRounding { get; init; } = ConversionRoundingPolicy.RoundAtTarget;

    /// <summary>
    /// Resolves the effective fractional-digit scale for a currency with <paramref name="currencyMinorUnits" /> minor
    /// units.
    /// </summary>
    /// <param name="currencyMinorUnits">The currency's declared minor-unit precision.</param>
    /// <returns>
    /// The number of fractional digits to round to, or <c>-1</c> when <see cref="ScalePolicy.Unrounded" /> defers
    /// rounding entirely.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="ScalePolicy" /> is not a defined value.</exception>
    /// <exception cref="ArgumentException">
    /// <see cref="ScalePolicy" /> is <see cref="ScalePolicy.Custom" /> but <see cref="CustomScale" /> is
    /// <see langword="null" />.
    /// </exception>
    public int ResolveScale(int currencyMinorUnits)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(ScalePolicy);

        return ScalePolicy switch
        {
            ScalePolicy.CurrencyMinorUnits => currencyMinorUnits,
            ScalePolicy.Unrounded => -1,
            _ => CustomScale ?? throw new ArgumentException(FinancialResourceStrings.Arg_Invalid_CustomScaleRequiresValue, nameof(CustomScale)),
        };
    }

    /// <summary>
    /// Rounds <paramref name="value" /> for a currency with <paramref name="currencyMinorUnits" /> minor units using
    /// this context's rounding strategy and resolved scale.
    /// </summary>
    /// <param name="value">The raw amount to round.</param>
    /// <param name="currencyMinorUnits">The currency's declared minor-unit precision.</param>
    /// <returns>
    /// The rounded amount, or <paramref name="value" /> unchanged when <see cref="ScalePolicy.Unrounded" /> is in
    /// effect.
    /// </returns>
    public decimal Round(decimal value, int currencyMinorUnits)
    {
        int scale = ResolveScale(currencyMinorUnits);
        return scale < 0 ? value : Rounding.Round(value, scale);
    }

    /// <summary>
    /// Validates that every policy member is a defined value and that <see cref="CustomScale" /> is supplied when
    /// required.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">A policy member is not a defined value.</exception>
    /// <exception cref="ArgumentException">
    /// <see cref="ScalePolicy" /> is <see cref="ScalePolicy.Custom" /> but <see cref="CustomScale" /> is
    /// <see langword="null" />, or it is out of the range 0 to 28.
    /// </exception>
    public void Validate()
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(ScalePolicy);
        ThrowHelper.ThrowIfEnumValueIsUndefined(CashRounding);
        ThrowHelper.ThrowIfEnumValueIsUndefined(Allocation);
        ThrowHelper.ThrowIfEnumValueIsUndefined(ConversionRounding);

        if (ScalePolicy == ScalePolicy.Custom)
        {
            if (CustomScale is null)
                throw new ArgumentException(FinancialResourceStrings.Arg_Invalid_CustomScaleRequiresValue, nameof(CustomScale));

            if ((uint)CustomScale.Value > 28u)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(CustomScale),
                    CustomScale.Value,
                    string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Arg_OutOfRange_CustomScale, CustomScale.Value));
            }
        }
    }
}
