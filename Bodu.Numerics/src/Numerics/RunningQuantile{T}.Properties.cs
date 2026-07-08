// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RunningQuantile{T}.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial struct RunningQuantile<T>
{
    /// <summary>
    /// Gets the quantile this estimator targets, strictly between 0 and 1.
    /// </summary>
    /// <value>The target probability; 0.5 for the <see langword="default" /> (median) estimator.</value>
    public readonly double Probability =>
        _probabilityOffset + 0.5;

    /// <summary>
    /// Gets the number of samples accumulated so far.
    /// </summary>
    /// <value>The sample count; zero for the empty estimator.</value>
    public readonly long Count =>
        _count;

    /// <summary>
    /// Gets a value indicating whether the estimator contains no samples.
    /// </summary>
    /// <value><see langword="true" /> when <see cref="Count" /> is zero; otherwise <see langword="false" />.</value>
    public readonly bool IsEmpty =>
        _count == 0;

    /// <summary>
    /// Gets the current estimate of the target quantile.
    /// </summary>
    /// <value>
    /// The linearly interpolated empirical quantile while fewer than five samples are held; the P² middle-marker
    /// estimate from the fifth sample onward.
    /// </value>
    /// <exception cref="InvalidOperationException">The estimator is empty.</exception>
    public readonly double Value
    {
        get
        {
            NumericsThrowHelper.ThrowIfNoSamples(_count);

            if (_count >= 5)
                return _heights[2];

            // Warm-up: the held samples are sorted, so interpolate the empirical quantile directly.
            var rank = (_count - 1) * Probability;
            var index = (int)rank;

            if (index + 1 >= _count)
                return _heights[(int)_count - 1];

            return _heights[index] + ((rank - index) * (_heights[index + 1] - _heights[index]));
        }
    }
}
