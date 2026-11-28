// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonetaryContextOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Holds the configuration-bindable settings of a <see cref="MonetaryContext" />, the form in which
/// <see cref="FinancialOptions" /> carries its default and named monetary contexts.
/// </summary>
/// <remarks>
/// Each property has the default of the <see cref="MonetaryContext" /> member it configures, so an options instance
/// with no setting changed builds a context equal to <see cref="MonetaryContext.Default" />. The settings bind from a
/// configuration section by name, the enumerations by member name; <see cref="ToMonetaryContext" /> validates them.
/// </remarks>
public sealed class MonetaryContextOptions
{
    /// <summary>The rounding strategies <see cref="ToMonetaryContext" /> assigns, indexed by <see cref="MonetaryRoundingMode" /> value.</summary>
    private static readonly IRoundingStrategy[] s_roundingStrategies =
    [
        MidpointRoundingStrategy.ToEven,
        MidpointRoundingStrategy.AwayFromZero,
        new MidpointRoundingStrategy(MidpointRounding.ToZero),
        new MidpointRoundingStrategy(MidpointRounding.ToNegativeInfinity),
        new MidpointRoundingStrategy(MidpointRounding.ToPositiveInfinity),
        StochasticRoundingStrategy.Shared,
    ];

    /// <summary>
    /// Gets or sets the rounding strategy of the context.
    /// </summary>
    /// <value>The rounding mode. The default is <see cref="MonetaryRoundingMode.ToEven" />.</value>
    public MonetaryRoundingMode Rounding { get; set; } = MonetaryRoundingMode.ToEven;

    /// <summary>
    /// Gets or sets the policy that determines the fractional-digit scale results are rounded to.
    /// </summary>
    /// <value>The scale policy. The default is <see cref="ScalePolicy.CurrencyMinorUnits" />.</value>
    public ScalePolicy ScalePolicy { get; set; } = ScalePolicy.CurrencyMinorUnits;

    /// <summary>
    /// Gets or sets the explicit scale used when <see cref="ScalePolicy" /> is <see cref="ScalePolicy.Custom" />.
    /// </summary>
    /// <value>
    /// The custom scale, or <see langword="null" /> when not applicable. The default is <see langword="null" />.
    /// </value>
    public int? CustomScale { get; set; }

    /// <summary>
    /// Gets or sets the cash-rounding policy applied to results.
    /// </summary>
    /// <value>The cash-rounding policy. The default is <see cref="CashRoundingPolicy.None" />.</value>
    public CashRoundingPolicy CashRounding { get; set; } = CashRoundingPolicy.None;

    /// <summary>
    /// Gets or sets the allocation policy for distributing an amount across shares.
    /// </summary>
    /// <value>The allocation policy. The default is <see cref="AllocationPolicy.LargestRemainder" />.</value>
    public AllocationPolicy Allocation { get; set; } = AllocationPolicy.LargestRemainder;

    /// <summary>
    /// Gets or sets the policy for when a currency conversion is rounded.
    /// </summary>
    /// <value>
    /// The conversion-rounding policy. The default is <see cref="ConversionRoundingPolicy.RoundAtTarget" />.
    /// </value>
    public ConversionRoundingPolicy ConversionRounding { get; set; } = ConversionRoundingPolicy.RoundAtTarget;

    /// <summary>
    /// Builds the <see cref="MonetaryContext" /> these settings describe, after validating them.
    /// </summary>
    /// <returns>The monetary context.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <see cref="Rounding" />, <see cref="ScalePolicy" />, <see cref="CashRounding" />, <see cref="Allocation" /> or
    /// <see cref="ConversionRounding" /> is not a defined value, or <see cref="ScalePolicy" /> is
    /// <see cref="ScalePolicy.Custom" /> and <see cref="CustomScale" /> is outside the range 0 to 28.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <see cref="ScalePolicy" /> is <see cref="ScalePolicy.Custom" /> but <see cref="CustomScale" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// The rounding strategy instances are shared, so two options instances with the same settings build equal
    /// contexts.
    /// </remarks>
    public MonetaryContext ToMonetaryContext()
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(Rounding);

        var context = new MonetaryContext
        {
            Rounding = s_roundingStrategies[(int)Rounding],
            ScalePolicy = ScalePolicy,
            CustomScale = CustomScale,
            CashRounding = CashRounding,
            Allocation = Allocation,
            ConversionRounding = ConversionRounding,
        };

        context.Validate();
        return context;
    }
}
