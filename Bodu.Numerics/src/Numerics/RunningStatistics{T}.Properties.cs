// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RunningStatistics{T}.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial struct RunningStatistics<T>
{
    /// <summary>
    /// Gets the number of samples accumulated so far.
    /// </summary>
    /// <value>The sample count; zero for the empty accumulator.</value>
    public readonly long Count =>
        _count;

    /// <summary>
    /// Gets a value indicating whether the accumulator contains no samples.
    /// </summary>
    /// <value><see langword="true" /> when <see cref="Count" /> is zero; otherwise <see langword="false" />.</value>
    public readonly bool IsEmpty =>
        _count == 0;

    /// <summary>
    /// Gets the smallest sample accumulated so far, tracked exactly in <typeparamref name="T" />.
    /// </summary>
    /// <value>The minimum sample.</value>
    /// <exception cref="InvalidOperationException">The accumulator is empty.</exception>
    public readonly T Minimum
    {
        get
        {
            NumericsThrowHelper.ThrowIfNoSamples(_count);

            return _minimum;
        }
    }

    /// <summary>
    /// Gets the largest sample accumulated so far, tracked exactly in <typeparamref name="T" />.
    /// </summary>
    /// <value>The maximum sample.</value>
    /// <exception cref="InvalidOperationException">The accumulator is empty.</exception>
    public readonly T Maximum
    {
        get
        {
            NumericsThrowHelper.ThrowIfNoSamples(_count);

            return _maximum;
        }
    }

    /// <summary>
    /// Gets the arithmetic mean of the accumulated samples.
    /// </summary>
    /// <value>The running mean, accumulated in <see cref="double" />.</value>
    /// <exception cref="InvalidOperationException">The accumulator is empty.</exception>
    public readonly double Mean
    {
        get
        {
            NumericsThrowHelper.ThrowIfNoSamples(_count);

            return _mean;
        }
    }

    /// <summary>
    /// Gets the population variance of the accumulated samples — the squared deviations divided by <see cref="Count" />.
    /// </summary>
    /// <value>The population variance; zero for a single sample.</value>
    /// <exception cref="InvalidOperationException">The accumulator is empty.</exception>
    public readonly double PopulationVariance
    {
        get
        {
            NumericsThrowHelper.ThrowIfNoSamples(_count);

            return _m2 / _count;
        }
    }

    /// <summary>
    /// Gets the sample (Bessel-corrected) variance of the accumulated samples — the squared deviations divided by
    /// <see cref="Count" /> − 1.
    /// </summary>
    /// <value>The unbiased sample variance.</value>
    /// <exception cref="InvalidOperationException">The accumulator holds fewer than two samples.</exception>
    public readonly double SampleVariance
    {
        get
        {
            if (_count < 2) throw new InvalidOperationException(NumericsResourceStrings.Op_Invalid_SampleVarianceRequiresTwo);

            return _m2 / (_count - 1);
        }
    }

    /// <summary>
    /// Gets the population standard deviation — the square root of <see cref="PopulationVariance" />.
    /// </summary>
    /// <value>The population standard deviation.</value>
    /// <exception cref="InvalidOperationException">The accumulator is empty.</exception>
    public readonly double PopulationStandardDeviation =>
        Math.Sqrt(PopulationVariance);

    /// <summary>
    /// Gets the sample standard deviation — the square root of <see cref="SampleVariance" />.
    /// </summary>
    /// <value>The sample standard deviation.</value>
    /// <exception cref="InvalidOperationException">The accumulator holds fewer than two samples.</exception>
    public readonly double SampleStandardDeviation =>
        Math.Sqrt(SampleVariance);
}
