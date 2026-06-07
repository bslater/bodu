// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericsThrowHelper.CallerExpression.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Bodu.Numerics;

internal static partial class NumericsThrowHelper
{
    /// <summary>
    /// Throws a <see cref="DivideByZeroException" /> when <paramref name="denominator" /> is zero.
    /// </summary>
    /// <typeparam name="TValue">The numeric type of the denominator being validated.</typeparam>
    /// <param name="denominator">The denominator value to validate.</param>
    /// <exception cref="DivideByZeroException">
    /// Thrown when <paramref name="denominator" /> is equal to zero.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfDenominatorZero<TValue>(TValue denominator)
        where TValue : INumberBase<TValue>
    {
        if (TValue.IsZero(denominator))
            throw new DivideByZeroException(NumericsResourceStrings.Math_DivideByZero_FractionDenominator);
    }

    /// <summary>
    /// Throws when <paramref name="policy" /> is not a defined <see cref="Serialization.NumericsJsonPolicy" /> member.
    /// </summary>
    /// <param name="policy">The policy value to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="policy" /> is not a defined value.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfNumericsJsonPolicyUndefined(
        Serialization.NumericsJsonPolicy policy,
        [CallerArgumentExpression(nameof(policy))] string? paramName = null)
    {
        if (policy is not Serialization.NumericsJsonPolicy.Strict
            and not Serialization.NumericsJsonPolicy.Lenient
            and not Serialization.NumericsJsonPolicy.Compact)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                policy,
                string.Format(
                    CultureInfo.InvariantCulture,
                    NumericsResourceStrings.Op_Invalid_NumericsJsonPolicyUndefined,
                    policy));
        }
    }
}
