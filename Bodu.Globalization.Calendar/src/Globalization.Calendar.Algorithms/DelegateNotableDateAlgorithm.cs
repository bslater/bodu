// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelegateNotableDateAlgorithm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Adapts a year-to-date delegate to the <see cref="INotableDateAlgorithm" /> contract, allowing a built-in calculator
/// (together with any parameters it captures, such as a time-zone offset or a festival key) to be registered as a
/// first-class algorithm.
/// </summary>
internal sealed class DelegateNotableDateAlgorithm
    : INotableDateAlgorithm
{
    /// <summary>
    /// The delegate that computes the occurrence for a Gregorian year.
    /// </summary>
    private readonly Func<int, DateOnly?> _calculate;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegateNotableDateAlgorithm" /> class.
    /// </summary>
    /// <param name="calculate">The delegate that computes the occurrence for a Gregorian year.</param>
    /// <exception cref="ArgumentNullException"><paramref name="calculate" /> is <see langword="null" />.</exception>
    public DelegateNotableDateAlgorithm(Func<int, DateOnly?> calculate)
    {
        ThrowHelper.ThrowIfNull(calculate);

        _calculate = calculate;
    }

    /// <inheritdoc />
    public DateOnly? Calculate(int year) =>
        _calculate(year);
}
