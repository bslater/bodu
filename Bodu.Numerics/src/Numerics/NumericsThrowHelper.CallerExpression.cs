// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericsThrowHelper.CallerExpression.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Bodu.Numerics;

internal static partial class NumericsThrowHelper
{
    /// <summary>
    /// Throws when <paramref name="denominator" /> is zero, the one fraction validation rule shared by the
    /// component-typed constructor and the <see cref="System.Numerics.BigInteger" /> factory.
    /// </summary>
    /// <typeparam name="TValue">The integer type carrying the candidate denominator.</typeparam>
    /// <param name="denominator">The denominator value to validate.</param>
    /// <exception cref="DivideByZeroException">Thrown when <paramref name="denominator" /> is zero.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfDenominatorZero<TValue>(TValue denominator)
        where TValue : INumberBase<TValue>
    {
        if (TValue.IsZero(denominator))
            throw new DivideByZeroException(NumericsResourceStrings.DivideByZero_DenominatorZero);
    }

    /// <summary>
    /// Throws when <paramref name="value" /> is not a finite number, the sample-admission rule shared by every
    /// statistics accumulator: NaN would poison the running moments irrecoverably, and an infinite sample makes both
    /// the Welford delta and windowed eviction arithmetic undefined.
    /// </summary>
    /// <typeparam name="TValue">The numeric type carrying the candidate sample.</typeparam>
    /// <param name="value">The sample value to validate.</param>
    /// <param name="paramName">The name of the argument being validated; supplied by the compiler.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is NaN or infinite.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfNotFinite<TValue>(
        TValue value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where TValue : INumberBase<TValue>
    {
        if (!TValue.IsFinite(value))
            throw new ArgumentException(NumericsResourceStrings.Arg_Invalid_NonFiniteSample, paramName);
    }

    /// <summary>
    /// Throws when a widened sample is no longer finite, the overflow rule shared by every statistics accumulator that
    /// converts samples to <see cref="double" />: unbounded integer types saturate to infinity rather than throwing
    /// from <c>double.CreateChecked</c>, so the result of the widening must be checked explicitly.
    /// </summary>
    /// <param name="widened">The sample value after widening to <see cref="double" />.</param>
    /// <exception cref="OverflowException">Thrown when <paramref name="widened" /> is not finite.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfWideningOverflowed(double widened)
    {
        if (!double.IsFinite(widened))
            throw new OverflowException(NumericsResourceStrings.Overflow_SampleOutsideDoubleRange);
    }

    /// <summary>
    /// Throws when <paramref name="count" /> is zero, the empty-accumulator rule shared by every statistics accumulator
    /// whose result properties are undefined before the first sample.
    /// </summary>
    /// <param name="count">The number of samples the accumulator currently holds.</param>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="count" /> is zero.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfNoSamples(long count)
    {
        if (count == 0)
            throw new InvalidOperationException(NumericsResourceStrings.Op_Invalid_EmptyAccumulator);
    }
}
