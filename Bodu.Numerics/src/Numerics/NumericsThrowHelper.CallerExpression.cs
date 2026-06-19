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
}
