// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StochasticRoundingStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// An <see cref="IRoundingStrategy" /> that rounds a value up or down probabilistically, with the probability of
/// rounding up equal to the fractional part discarded at the target scale. Over many roundings the expected value
/// equals the raw amount, so the convention is statistically unbiased and does not accumulate the directional drift
/// that a fixed midpoint rule can introduce across a long series of operations.
/// </summary>
/// <remarks>
/// <para>
/// For a value whose magnitude beyond <c>scale</c> digits has fractional part <c>f</c> (in the half-open interval
/// <c>[0, 1)</c>), this strategy rounds up with probability <c>f</c> and down with probability <c>1 - f</c>. A value
/// already exact at the target scale is returned unchanged. The rule is applied on the number line (toward positive
/// infinity for "up"), so negative amounts are unbiased in the same way as positive amounts.
/// </para>
/// <para>
/// Randomness is drawn from an injected sampler returning a value in <c>[0, 1)</c>; the parameterless constructor uses
/// <see cref="Random.Shared" />. Because the result depends on the sampler, this strategy is inherently
/// non-deterministic and two roundings of the same input may differ — supply a fixed-sequence sampler to make a test
/// deterministic. Unlike <see cref="MidpointRoundingStrategy" />, this convention cannot be expressed as a
/// <see cref="MidpointRounding" /> mode, which is why it is a distinct implementation of the
/// <see cref="IRoundingStrategy" /> seam rather than another mode of the midpoint strategy.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Unbiased rounding for a stochastic allocation policy.
/// var context = new MonetaryContext { Rounding = StochasticRoundingStrategy.Shared };
///
/// // Deterministic in a test: a sampler that always draws 0 rounds every non-exact value up.
/// var up = new StochasticRoundingStrategy(() => 0.0);
/// decimal r = up.Round(1.001m, 2); // 1.01
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class StochasticRoundingStrategy
    : IRoundingStrategy
{
    /// <summary>The largest scale the strategy accepts, matching the fractional-digit range of <see cref="decimal" />.</summary>
    private const int MaxScale = 28;

    /// <summary>The shared strategy backed by <see cref="Random.Shared" />, safe for concurrent use.</summary>
    public static readonly StochasticRoundingStrategy Shared = new();

    /// <summary>Powers of ten used to shift a value to and from the target scale, indexed by scale.</summary>
    private static readonly decimal[] s_powersOfTen = BuildPowersOfTen();

    /// <summary>The sampler returning a value in <c>[0, 1)</c> that decides the rounding direction.</summary>
    private readonly Func<double> _sampler;

    /// <summary>
    /// Initializes a new instance of the <see cref="StochasticRoundingStrategy" /> class backed by
    /// <see cref="Random.Shared" />.
    /// </summary>
    public StochasticRoundingStrategy()
        : this(static () => Random.Shared.NextDouble())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StochasticRoundingStrategy" /> class backed by the supplied
    /// sampler.
    /// </summary>
    /// <param name="sampler">
    /// A function returning a value in the half-open interval <c>[0, 1)</c> for each rounding.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sampler" /> is <see langword="null" />.
    /// </exception>
    public StochasticRoundingStrategy(Func<double> sampler)
    {
        ThrowHelper.ThrowIfNull(sampler);

        _sampler = sampler;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="scale" /> is negative or exceeds 28.</exception>
    public decimal Round(decimal value, int scale)
    {
        ThrowHelper.ThrowIfOutOfRange(scale, 0, MaxScale);

        decimal factor = s_powersOfTen[scale];
        decimal scaled = value * factor;
        decimal lower = Math.Floor(scaled);
        decimal fraction = scaled - lower;

        if (fraction == 0m)
            return value;

        // Round up when the draw falls below the discarded fraction, giving an expected value equal to the raw amount.
        decimal rounded = _sampler() < (double)fraction ? lower + 1m : lower;

        return rounded / factor;
    }

    /// <summary>
    /// Builds the power-of-ten lookup table spanning every scale <see cref="decimal" /> supports.
    /// </summary>
    /// <returns>An array whose element at index <c>i</c> is <c>10</c> raised to the power <c>i</c>.</returns>
    private static decimal[] BuildPowersOfTen()
    {
        var powers = new decimal[MaxScale + 1];
        decimal power = 1m;

        for (int i = 0; i <= MaxScale; i++)
        {
            powers[i] = power;
            if (i < MaxScale)
                power *= 10m;
        }

        return powers;
    }
}
