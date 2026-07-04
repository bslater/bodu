// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimalTests.GenericMath.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public partial class BigDecimalTests
{
    /// <summary>
    /// Verifies that <see cref="BigDecimal" /> composes with <see cref="INumber{TSelf}" />-constrained generic code and
    /// exposes the base-ten radix.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenSummedGenerically_ShouldProduceExactResult()
    {
        BigDecimal sum = GenericSum(BD(1, 1), BD(2, 1), BD(3, 1));   // 0.1 + 0.2 + 0.3

        Assert.AreEqual("0.6", sum.ToString());
        Assert.AreEqual(10, GenericRadix<BigDecimal>());
        Assert.AreEqual(BigDecimal.One, MultiplicativeIdentityOf<BigDecimal>());
        Assert.AreEqual(BigDecimal.MinusOne, NegativeOneOf<BigDecimal>());
    }

    /// <summary>
    /// Verifies the <see cref="INumberBase{TSelf}" /> classification predicates through the static surface.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenClassifyingValues_ShouldReportExpectedFlags()
    {
        Assert.IsTrue(IsIntegerOf(BD(100, 0)));
        Assert.IsFalse(IsIntegerOf(BD(5, 1)));
        Assert.IsTrue(IsEvenIntegerOf(BD(4, 0)));
        Assert.IsTrue(IsOddIntegerOf(BD(3, 0)));
        Assert.IsTrue(IsNegativeOf(BD(-1, 0)));
        Assert.IsTrue(IsZeroOf(BigDecimal.Zero));
    }

    /// <summary>
    /// Verifies that the generic-math conversion factories round-trip <see cref="BigDecimal" /> to and from other
    /// numeric types.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenConverting_ShouldRoundTripThroughOtherTypes()
    {
        Assert.AreEqual(BD(5, 0), CreateCheckedOf<BigDecimal, int>(5));
        Assert.AreEqual(BD(25, 2), CreateCheckedOf<BigDecimal, decimal>(0.25m));
        Assert.AreEqual(5, int.CreateChecked(BD(5, 0)));
        Assert.AreEqual(3, int.CreateTruncating(BD(35, 1)));   // 3.5 truncates to 3
    }

    /// <summary>
    /// Verifies that <see cref="INumber{TSelf}.Clamp" /> restricts a value to the supplied bounds.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenClamping_ShouldRestrictToBounds()
    {
        Assert.AreEqual(BD(1, 0), ClampOf(BD(5, 1), BD(1, 0), BD(9, 0)));   // 0.5 clamped up to 1
        Assert.AreEqual(BD(9, 0), ClampOf(BD(100, 0), BD(1, 0), BD(9, 0))); // 100 clamped down to 9
        Assert.AreEqual(BD(5, 0), ClampOf(BD(5, 0), BD(1, 0), BD(9, 0)));   // in range
    }

    /// <summary>
    /// Sums values through the <see cref="INumber{TSelf}" /> generic-math surface.
    /// </summary>
    /// <typeparam name="T">A generic-math numeric type.</typeparam>
    /// <param name="values">The values to sum.</param>
    /// <returns>The generic-math sum.</returns>
    private static T GenericSum<T>(params T[] values)
        where T : INumber<T>
    {
        T accumulator = T.Zero;
        foreach (T value in values)
            accumulator += value;

        return accumulator;
    }

    /// <summary>
    /// Reads <see cref="INumberBase{TSelf}.Radix" /> through the generic constraint.
    /// </summary>
    /// <typeparam name="T">A generic-math numeric type.</typeparam>
    /// <returns>The type's radix.</returns>
    private static int GenericRadix<T>()
        where T : INumberBase<T> =>
        T.Radix;

    /// <summary>
    /// Reads <see cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" /> through the constraint.
    /// </summary>
    /// <typeparam name="T">A generic-math numeric type.</typeparam>
    /// <returns>The multiplicative identity.</returns>
    private static T MultiplicativeIdentityOf<T>()
        where T : IMultiplicativeIdentity<T, T> =>
        T.MultiplicativeIdentity;

    /// <summary>
    /// Reads <see cref="ISignedNumber{TSelf}.NegativeOne" /> through the constraint.
    /// </summary>
    /// <typeparam name="T">A signed generic-math numeric type.</typeparam>
    /// <returns>The negative-one constant.</returns>
    private static T NegativeOneOf<T>()
        where T : ISignedNumber<T> =>
        T.NegativeOne;

    /// <summary>
    /// Determines whether a number is an integer using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="T">A generic-math numeric type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns><see langword="true" /> if the value is an integer.</returns>
    private static bool IsIntegerOf<T>(T value)
        where T : INumberBase<T> =>
        T.IsInteger(value);

    /// <summary>
    /// Determines whether a number is an even integer using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="T">A generic-math numeric type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns><see langword="true" /> if the value is an even integer.</returns>
    private static bool IsEvenIntegerOf<T>(T value)
        where T : INumberBase<T> =>
        T.IsEvenInteger(value);

    /// <summary>
    /// Determines whether a number is an odd integer using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="T">A generic-math numeric type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns><see langword="true" /> if the value is an odd integer.</returns>
    private static bool IsOddIntegerOf<T>(T value)
        where T : INumberBase<T> =>
        T.IsOddInteger(value);

    /// <summary>
    /// Determines whether a number is negative using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="T">A generic-math numeric type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns><see langword="true" /> if the value is negative.</returns>
    private static bool IsNegativeOf<T>(T value)
        where T : INumberBase<T> =>
        T.IsNegative(value);

    /// <summary>
    /// Determines whether a number is zero using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="T">A generic-math numeric type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns><see langword="true" /> if the value is zero.</returns>
    private static bool IsZeroOf<T>(T value)
        where T : INumberBase<T> =>
        T.IsZero(value);

    /// <summary>
    /// Creates a <typeparamref name="T" /> from a source value using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="T">The target generic-math numeric type.</typeparam>
    /// <typeparam name="TSource">The source numeric type.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted value.</returns>
    private static T CreateCheckedOf<T, TSource>(TSource value)
        where T : INumberBase<T>
        where TSource : INumberBase<TSource> =>
        T.CreateChecked(value);

    /// <summary>
    /// Clamps a number to an inclusive range using the <see cref="INumber{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="T">A generic-math numeric type.</typeparam>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The inclusive upper bound.</param>
    /// <returns>The clamped value.</returns>
    private static T ClampOf<T>(T value, T min, T max)
        where T : INumber<T> =>
        T.Clamp(value, min, max);
}
