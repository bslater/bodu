// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonetaryContext.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial;

/// <summary>
/// Carries the rounding and scaling policy applied at the monetary operations that accept one - typed multiplication,
/// runtime conversion through an <see cref="ExchangeRates.ExchangeRate" />, and the conversion of a high-precision
/// calculation back to a settlement value.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="MonetaryContext" /> is supplied to operations rather than embedded in a <see cref="Money" /> value, so
/// the same amount can participate in calculations under different policies without carrying mutable state. Operations
/// that accept a nullable context treat <see langword="null" /> as <see cref="Default" />, preserving the historical
/// banker's-rounding behaviour.
/// </para>
/// <para>
/// The members that accept a context are <see cref="Money{TCurrency}.Multiply(decimal, MonetaryContext)" />, which
/// applies its <see cref="Rounding" /> strategy;
/// <see cref="Money.Convert(ExchangeRates.ExchangeRate, MonetaryContext?)" /> and
/// <see cref="Money.ConvertWithResult(ExchangeRates.ExchangeRate, MonetaryContext?)" />; and
/// <see cref="CalculatedMoney.RoundToMoney(MonetaryContext?)" />, which applies the rounding strategy, the scale
/// policy, and the cash-rounding policy. No division or allocation member accepts a context, and no operation consults
/// <see cref="Allocation" /> or <see cref="ConversionRounding" /> yet: <see cref="Validate" /> checks them, but
/// allocation always uses the largest-remainder method and conversion always rounds at the target.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Financial;
/// using Bodu.Financial.Currencies;
///
/// // Start from the default and override only the policies you need.
/// MonetaryContext retail = MonetaryContext.Default with
/// {
///     Rounding = new MidpointRoundingStrategy(MidpointRounding.AwayFromZero),
///     Allocation = AllocationPolicy.LargestRemainder,
/// };
///
/// // Apply it at a settlement boundary.
/// var calc = new CalculatedMoney(2.125m, CurrencyCode.USD);
/// Money settled = calc.RoundToMoney(retail);   // 2.13 USD (rounded away from zero)
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed record MonetaryContext
{
    /// <summary>
    /// Gets the shared default context: banker's rounding to the currency's minor units, no cash rounding,
    /// largest-remainder allocation, and rounding conversions at the target scale.
    /// </summary>
    /// <value>The default <see cref="MonetaryContext" />.</value>
    public static MonetaryContext Default { get; } = new();

    /// <summary>
    /// Gets the rounding strategy applied when reducing a computed amount to the resolved scale.
    /// </summary>
    /// <value>The rounding strategy; never <see langword="null" />.</value>
    public IRoundingStrategy Rounding { get; init; } = MidpointRoundingStrategy.ToEven;

    /// <summary>
    /// Gets the policy that determines the fractional-digit scale results are rounded to.
    /// </summary>
    /// <value>The configured <see cref="ScalePolicy" />.</value>
    public ScalePolicy ScalePolicy { get; init; } = ScalePolicy.CurrencyMinorUnits;

    /// <summary>
    /// Gets the explicit scale used when <see cref="ScalePolicy" /> is <see cref="ScalePolicy.Custom" />.
    /// </summary>
    /// <value>The custom scale, or <see langword="null" /> when not applicable.</value>
    public int? CustomScale { get; init; }

    /// <summary>
    /// Gets the cash-rounding policy applied to results.
    /// </summary>
    /// <value>The configured <see cref="CashRoundingPolicy" />.</value>
    public CashRoundingPolicy CashRounding { get; init; } = CashRoundingPolicy.None;

    /// <summary>
    /// Gets the allocation policy for distributing an amount across shares.
    /// </summary>
    /// <value>The configured <see cref="AllocationPolicy" />.</value>
    /// <remarks>
    /// Reserved: <see cref="Validate" /> checks the value, but no allocation member accepts a context, and allocation
    /// always uses <see cref="AllocationPolicy.LargestRemainder" />.
    /// </remarks>
    public AllocationPolicy Allocation { get; init; } = AllocationPolicy.LargestRemainder;

    /// <summary>
    /// Gets the policy for when a currency conversion is rounded.
    /// </summary>
    /// <value>The configured <see cref="ConversionRoundingPolicy" />.</value>
    /// <remarks>
    /// Reserved: <see cref="Validate" /> checks the value, but no conversion consults it, and conversions always round
    /// at the target, as <see cref="ConversionRoundingPolicy.RoundAtTarget" /> describes.
    /// </remarks>
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
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="currencyMinorUnits" /> is outside the range 0 to 28, whatever the scale policy;
    /// <see cref="ScalePolicy" /> is not a defined value; or <see cref="ScalePolicy" /> is
    /// <see cref="ScalePolicy.Custom" /> and <see cref="CustomScale" /> is outside the range 0 to 28.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <see cref="ScalePolicy" /> is <see cref="ScalePolicy.Custom" /> but <see cref="CustomScale" /> is
    /// <see langword="null" />.
    /// </exception>
    public int ResolveScale(int currencyMinorUnits)
    {
        ThrowHelper.ThrowIfOutOfRange(currencyMinorUnits, 0, 28);
        ThrowHelper.ThrowIfEnumValueIsUndefined(ScalePolicy);

        return ScalePolicy switch
        {
            ScalePolicy.CurrencyMinorUnits => currencyMinorUnits,
            ScalePolicy.Unrounded => -1,
            _ => GetValidatedCustomScale(),
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
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="currencyMinorUnits" /> is outside the range 0 to 28, whatever the scale policy;
    /// <see cref="ScalePolicy" /> is not a defined value; or <see cref="ScalePolicy" /> is
    /// <see cref="ScalePolicy.Custom" /> and <see cref="CustomScale" /> is outside the range 0 to 28.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <see cref="ScalePolicy" /> is <see cref="ScalePolicy.Custom" /> but <see cref="CustomScale" /> is
    /// <see langword="null" />.
    /// </exception>
    public decimal Round(decimal value, int currencyMinorUnits)
    {
        int scale = ResolveScale(currencyMinorUnits);
        return scale < 0 ? value : Rounding.Round(value, scale);
    }

    /// <summary>
    /// Validates that every policy member is a defined value and that <see cref="CustomScale" /> is supplied when
    /// required.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A policy member is not a defined value, or <see cref="ScalePolicy" /> is <see cref="ScalePolicy.Custom" /> and
    /// <see cref="CustomScale" /> is outside the range 0 to 28.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <see cref="ScalePolicy" /> is <see cref="ScalePolicy.Custom" /> but <see cref="CustomScale" /> is
    /// <see langword="null" />.
    /// </exception>
    public void Validate()
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(ScalePolicy);
        ThrowHelper.ThrowIfEnumValueIsUndefined(CashRounding);
        ThrowHelper.ThrowIfEnumValueIsUndefined(Allocation);
        ThrowHelper.ThrowIfEnumValueIsUndefined(ConversionRounding);

        if (ScalePolicy == ScalePolicy.Custom)
            _ = GetValidatedCustomScale();
    }

    /// <summary>
    /// Returns <see cref="CustomScale" /> after checking that it is supplied and within the range 0 to 28.
    /// </summary>
    /// <returns>The custom scale.</returns>
    /// <exception cref="ArgumentException"><see cref="CustomScale" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <see cref="CustomScale" /> is outside the range 0 to 28.
    /// </exception>
    private int GetValidatedCustomScale()
    {
        if (CustomScale is not int scale)
            throw new ArgumentException(FinancialResourceStrings.Arg_Invalid_CustomScaleRequiresValue, nameof(CustomScale));

        if ((uint)scale > 28u)
        {
            throw new ArgumentOutOfRangeException(
                nameof(CustomScale),
                scale,
                string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Arg_OutOfRange_CustomScale, scale));
        }

        return scale;
    }
}
