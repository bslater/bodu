// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComplexTests.Oracle.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using Bodu.Test;
using Bcl = System.Numerics.Complex;

namespace Bodu.Numerics;

public partial class ComplexTests
{
    /// <summary>Finite component values used to build the oracle sweep, including both signed zeros.</summary>
    private static readonly double[] FiniteParts = [-3.5, -1.0, -0.0, 0.0, 0.75, 2.0, 5.25];

    /// <summary>Component values including the non-finite specials, used for exactness and predicate parity.</summary>
    private static readonly double[] EdgeParts =
        [double.NegativeInfinity, -2.0, -0.0, 0.0, 1.5, double.PositiveInfinity, double.NaN];

    /// <summary>Moderate component values used for the transcendental functions, avoiding overflow noise.</summary>
    private static readonly double[] FunctionParts = [-2.0, -0.5, 0.0, 0.5, 1.0, 2.5];

    /// <summary>
    /// Component values for the inverse-trigonometric parity sweep, spanning the interior, the unit-boundary and
    /// beyond-unit branch-cut region, and both signed zeros.
    /// </summary>
    private static readonly double[] InverseTrigParts =
        [-3.0, -2.0, -1.5, -1.0, -0.5, -0.0, 0.0, 0.5, 1.0, 1.5, 2.0, 3.0];

    /// <summary>
    /// Component values used for the transcendental edge sweep: signed zeros, the non-finite specials, values that
    /// straddle the <c>Tan</c>/<c>Tanh</c> large-argument threshold, and very large / very small magnitudes.
    /// </summary>
    private static readonly double[] EdgeFunctionParts =
    [
        double.NegativeInfinity, -1000.0, -1.5, -0.0, 0.0, 1.5, 1000.0, double.PositiveInfinity, double.NaN,
        1e300, 1e-300,
    ];

    /// <summary>
    /// Verifies that the arithmetic operators agree bit for bit with <see cref="System.Numerics.Complex" /> across a
    /// component sweep that includes signed zeros and the non-finite specials.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void ArithmeticOperators_AgainstBclOracle_ShouldMatchBitForBit()
    {
        foreach (Complex<double> ma in EdgeValues())
        {
            Bcl ba = ToBcl(ma);
            foreach (Complex<double> mb in EdgeValues())
            {
                Bcl bb = ToBcl(mb);

                AssertBitExact(ma + mb, ba + bb, $"({ma}) + ({mb})");
                AssertBitExact(ma - mb, ba - bb, $"({ma}) - ({mb})");
                AssertBitExact(ma * mb, ba * bb, $"({ma}) * ({mb})");
                AssertBitExact(ma / mb, ba / bb, $"({ma}) / ({mb})");
            }
        }
    }

    /// <summary>
    /// Verifies that negation, conjugation, reciprocal, and the magnitude agree bit for bit with
    /// <see cref="System.Numerics.Complex" />, including the reciprocal-of-zero convention.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void UnaryOperations_AgainstBclOracle_ShouldMatchBitForBit()
    {
        foreach (Complex<double> m in EdgeValues())
        {
            Bcl b = ToBcl(m);

            AssertBitExact(-m, -b, $"-({m})");
            AssertBitExact(Complex<double>.Conjugate(m), Bcl.Conjugate(b), $"conj({m})");
            AssertBitExact(Complex<double>.Reciprocal(m), Bcl.Reciprocal(b), $"recip({m})");

            // Abs/Magnitude agrees to within a small tolerance rather than bit for bit: the scaled hypotenuse can
            // round differently from the framework's private helper by up to one ULP.
            AssertComponentClose(Complex<double>.Abs(m), Bcl.Abs(b), $"abs({m})", 1e-15);
        }
    }

    /// <summary>
    /// Verifies that every <see cref="System.Numerics.INumberBase{TSelf}" /> classification predicate agrees with
    /// <see cref="System.Numerics.Complex" /> across the edge sweep.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void NumberBasePredicates_AgainstBclOracle_ShouldMatchExactly()
    {
        string[] names =
        [
            "IsCanonical", "IsComplexNumber", "IsEvenInteger", "IsFinite", "IsImaginaryNumber", "IsInfinity",
            "IsInteger", "IsNaN", "IsNegative", "IsNegativeInfinity", "IsNormal", "IsOddInteger", "IsPositive",
            "IsPositiveInfinity", "IsRealNumber", "IsSubnormal", "IsZero",
        ];

        foreach (Complex<double> m in EdgeValues())
        {
            bool[] mine = Predicates(m);
            bool[] bcl = Predicates(ToBcl(m));

            for (int i = 0; i < names.Length; i++)
                Assert.AreEqual(bcl[i], mine[i], $"{names[i]}({m})");
        }
    }

    /// <summary>
    /// Verifies that the magnitude-selecting <see cref="System.Numerics.INumberBase{TSelf}" /> members agree with
    /// <see cref="System.Numerics.Complex" /> whenever the two operands have distinct magnitudes — the meaningful
    /// contract. Selection between two exactly-equal magnitudes is implementation-defined for a non-ordered complex
    /// type and is exercised separately in the generic-math tests.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void MagnitudeSelectors_AgainstBclOracle_ShouldMatchWhenMagnitudesDiffer()
    {
        foreach (Complex<double> ma in FiniteValues())
        {
            Bcl ba = ToBcl(ma);
            foreach (Complex<double> mb in FiniteValues())
            {
                if (Complex<double>.Abs(ma) == Complex<double>.Abs(mb))
                    continue;

                Bcl bb = ToBcl(mb);

                AssertBitExact(MaxMagnitude(ma, mb), MaxMagnitude(ba, bb), $"maxMag({ma},{mb})");
                AssertBitExact(MaxMagnitudeNumber(ma, mb), MaxMagnitudeNumber(ba, bb), $"maxMagNum({ma},{mb})");
                AssertBitExact(MinMagnitude(ma, mb), MinMagnitude(ba, bb), $"minMag({ma},{mb})");
                AssertBitExact(MinMagnitudeNumber(ma, mb), MinMagnitudeNumber(ba, bb), $"minMagNum({ma},{mb})");
            }
        }
    }

    /// <summary>
    /// Verifies that the transcendental functions agree with <see cref="System.Numerics.Complex" /> to within
    /// floating-point tolerance across the moderate component sweep.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Functions_AgainstBclOracle_ShouldMatchWithinTolerance()
    {
        foreach (Complex<double> m in FunctionValues())
        {
            Bcl b = ToBcl(m);

            AssertClose(Complex<double>.Sqrt(m), Bcl.Sqrt(b), $"sqrt({m})");
            AssertClose(Complex<double>.Exp(m), Bcl.Exp(b), $"exp({m})");
            AssertClose(Complex<double>.Sin(m), Bcl.Sin(b), $"sin({m})");
            AssertClose(Complex<double>.Cos(m), Bcl.Cos(b), $"cos({m})");
            AssertClose(Complex<double>.Tan(m), Bcl.Tan(b), $"tan({m})");
            AssertClose(Complex<double>.Sinh(m), Bcl.Sinh(b), $"sinh({m})");
            AssertClose(Complex<double>.Cosh(m), Bcl.Cosh(b), $"cosh({m})");
            AssertClose(Complex<double>.Tanh(m), Bcl.Tanh(b), $"tanh({m})");

            if (m != Complex<double>.Zero)
            {
                AssertClose(Complex<double>.Log(m), Bcl.Log(b), $"log({m})");
                AssertClose(Complex<double>.Log10(m), Bcl.Log10(b), $"log10({m})");
            }
        }
    }

    /// <summary>
    /// Verifies that the transcendental functions agree with <see cref="System.Numerics.Complex" /> across the edge
    /// domain — signed zeros, the non-finite specials, and very large and very small magnitudes — that the moderate
    /// sweep does not reach. This exercises the <c>Tan</c>/<c>Tanh</c> large-argument branch, the <c>Sqrt</c>
    /// overflow-rescale and infinity branches, and every function's non-finite handling. The comparison tolerates a
    /// non-finite result (an infinity is matched exactly, a NaN is skipped) so only a genuine finite divergence fails.
    /// A NaN <em>input</em> component is excluded, since a transcendental of a NaN-bearing argument has no well-defined
    /// value and its exact NaN/infinity arrangement is implementation-defined rather than part of the contract.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void FunctionsEdge_AgainstBclOracle_ShouldMatchWithinTolerance()
    {
        var square = new Complex<double>(2.0, 0.0);

        foreach (Complex<double> m in EdgeFunctionValues())
        {
            if (double.IsNaN(m.Real) || double.IsNaN(m.Imaginary))
                continue;

            Bcl b = ToBcl(m);

            AssertClose(Complex<double>.Sqrt(m), Bcl.Sqrt(b), $"sqrt({m})", 1e-9);
            AssertClose(Complex<double>.Exp(m), Bcl.Exp(b), $"exp({m})", 1e-9);
            AssertClose(Complex<double>.Log(m), Bcl.Log(b), $"log({m})", 1e-9);
            AssertClose(Complex<double>.Log10(m), Bcl.Log10(b), $"log10({m})", 1e-9);
            AssertClose(Complex<double>.Pow(m, square), Bcl.Pow(b, ToBcl(square)), $"pow({m},2)", 1e-9);
            AssertClose(Complex<double>.Sin(m), Bcl.Sin(b), $"sin({m})", 1e-9);
            AssertClose(Complex<double>.Cos(m), Bcl.Cos(b), $"cos({m})", 1e-9);
            AssertClose(Complex<double>.Tan(m), Bcl.Tan(b), $"tan({m})", 1e-9);
            AssertClose(Complex<double>.Sinh(m), Bcl.Sinh(b), $"sinh({m})", 1e-9);
            AssertClose(Complex<double>.Cosh(m), Bcl.Cosh(b), $"cosh({m})", 1e-9);
            AssertClose(Complex<double>.Tanh(m), Bcl.Tanh(b), $"tanh({m})", 1e-9);
        }
    }

    /// <summary>
    /// Verifies that the inverse trigonometric functions satisfy their defining identities to within tolerance, since
    /// they are evaluated from closed forms rather than in lock-step with <see cref="System.Numerics.Complex" />.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void InverseTrigFunctions_ShouldSatisfyDefiningIdentities()
    {
        // Interior points, kept away from the ±1 (asin/acos) and ±i (atan) branch points where the round-trip
        // identities are undefined.
        Complex<double>[] interior =
        [
            new(0.3, 0.4), new(-0.3, 0.4), new(0.5, -0.2), new(0.1, 0.1), new(-0.4, -0.3), new(0.6, 0.1),
        ];

        foreach (Complex<double> m in interior)
        {
            AssertClose(Complex<double>.Sin(Complex<double>.Asin(m)), ToBcl(m), $"sin(asin({m}))", 1e-7);
            AssertClose(Complex<double>.Cos(Complex<double>.Acos(m)), ToBcl(m), $"cos(acos({m}))", 1e-7);
            AssertClose(Complex<double>.Tan(Complex<double>.Atan(m)), ToBcl(m), $"tan(atan({m}))", 1e-7);
        }
    }

    /// <summary>
    /// Verifies that <see cref="Complex{T}.Asin" />, <see cref="Complex{T}.Acos" />, and <see cref="Complex{T}.Atan" />
    /// agree with <see cref="System.Numerics.Complex" /> across the interior, the branch cuts (the real axis outside
    /// <c>[-1, 1]</c> for asin / acos, the imaginary axis outside <c>[-i, i]</c> for atan), the branch-cut signed-zero
    /// sides, and the <c>atan(±i)</c> singularities — so the branch-cut placement is pinned against the framework, not
    /// merely a self-consistent closed form.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void InverseTrigFunctions_AgainstBclOracle_ShouldMatchWithinTolerance()
    {
        foreach (double re in InverseTrigParts)
        {
            foreach (double im in InverseTrigParts)
            {
                var m = new Complex<double>(re, im);
                Bcl b = ToBcl(m);

                AssertClose(Complex<double>.Asin(m), Bcl.Asin(b), $"asin({m})", 1e-12);
                AssertClose(Complex<double>.Acos(m), Bcl.Acos(b), $"acos({m})", 1e-12);
                AssertClose(Complex<double>.Atan(m), Bcl.Atan(b), $"atan({m})", 1e-12);
            }
        }
    }

    /// <summary>Converts a <see cref="Complex{T}" /> to the framework <see cref="System.Numerics.Complex" />.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The equivalent framework value.</returns>
    private static Bcl ToBcl(Complex<double> value) =>
        new(value.Real, value.Imaginary);

    /// <summary>Enumerates the finite complex values built from every ordered pair of finite parts.</summary>
    /// <returns>The finite sweep values.</returns>
    private static IEnumerable<Complex<double>> FiniteValues()
    {
        foreach (double re in FiniteParts)
            foreach (double im in FiniteParts)
                yield return new Complex<double>(re, im);
    }

    /// <summary>Enumerates the complex values built from every ordered pair of edge parts.</summary>
    /// <returns>The edge sweep values.</returns>
    private static IEnumerable<Complex<double>> EdgeValues()
    {
        foreach (double re in EdgeParts)
            foreach (double im in EdgeParts)
                yield return new Complex<double>(re, im);
    }

    /// <summary>Enumerates the moderate complex values used for the transcendental sweep.</summary>
    /// <returns>The function sweep values.</returns>
    private static IEnumerable<Complex<double>> FunctionValues()
    {
        foreach (double re in FunctionParts)
            foreach (double im in FunctionParts)
                yield return new Complex<double>(re, im);
    }

    /// <summary>Enumerates the edge-domain complex values used for the transcendental edge sweep.</summary>
    /// <returns>The edge function sweep values.</returns>
    private static IEnumerable<Complex<double>> EdgeFunctionValues()
    {
        foreach (double re in EdgeFunctionParts)
            foreach (double im in EdgeFunctionParts)
                yield return new Complex<double>(re, im);
    }

    /// <summary>Evaluates the full predicate vector for a value through its generic-math interface.</summary>
    /// <typeparam name="TC">The complex type under test.</typeparam>
    /// <param name="v">The value to classify.</param>
    /// <returns>The predicate results in a fixed order.</returns>
    private static bool[] Predicates<TC>(TC v)
        where TC : INumberBase<TC> =>
    [
        TC.IsCanonical(v), TC.IsComplexNumber(v), TC.IsEvenInteger(v), TC.IsFinite(v), TC.IsImaginaryNumber(v),
        TC.IsInfinity(v), TC.IsInteger(v), TC.IsNaN(v), TC.IsNegative(v), TC.IsNegativeInfinity(v), TC.IsNormal(v),
        TC.IsOddInteger(v), TC.IsPositive(v), TC.IsPositiveInfinity(v), TC.IsRealNumber(v), TC.IsSubnormal(v),
        TC.IsZero(v),
    ];

    /// <summary>Invokes the generic-math maximum-magnitude selector.</summary>
    /// <typeparam name="TC">The complex type under test.</typeparam>
    /// <param name="x">The first value.</param>
    /// <param name="y">The second value.</param>
    /// <returns>The selected value.</returns>
    private static TC MaxMagnitude<TC>(TC x, TC y)
        where TC : INumberBase<TC> =>
        TC.MaxMagnitude(x, y);

    /// <summary>Invokes the generic-math NaN-ignoring maximum-magnitude selector.</summary>
    /// <typeparam name="TC">The complex type under test.</typeparam>
    /// <param name="x">The first value.</param>
    /// <param name="y">The second value.</param>
    /// <returns>The selected value.</returns>
    private static TC MaxMagnitudeNumber<TC>(TC x, TC y)
        where TC : INumberBase<TC> =>
        TC.MaxMagnitudeNumber(x, y);

    /// <summary>Invokes the generic-math minimum-magnitude selector.</summary>
    /// <typeparam name="TC">The complex type under test.</typeparam>
    /// <param name="x">The first value.</param>
    /// <param name="y">The second value.</param>
    /// <returns>The selected value.</returns>
    private static TC MinMagnitude<TC>(TC x, TC y)
        where TC : INumberBase<TC> =>
        TC.MinMagnitude(x, y);

    /// <summary>Invokes the generic-math NaN-ignoring minimum-magnitude selector.</summary>
    /// <typeparam name="TC">The complex type under test.</typeparam>
    /// <param name="x">The first value.</param>
    /// <param name="y">The second value.</param>
    /// <returns>The selected value.</returns>
    private static TC MinMagnitudeNumber<TC>(TC x, TC y)
        where TC : INumberBase<TC> =>
        TC.MinMagnitudeNumber(x, y);

    /// <summary>Asserts that a computed value matches the framework result bit for bit in both components.</summary>
    /// <param name="mine">The value produced by <see cref="Complex{T}" />.</param>
    /// <param name="bcl">The framework value to match.</param>
    /// <param name="label">A label identifying the case on failure.</param>
    private static void AssertBitExact(Complex<double> mine, Bcl bcl, string label)
    {
        AssertComponentBits(mine.Real, bcl.Real, $"{label}.Real");
        AssertComponentBits(mine.Imaginary, bcl.Imaginary, $"{label}.Imaginary");
    }

    /// <summary>Asserts that two components are bit-identical, treating any two NaNs as equal.</summary>
    /// <param name="mine">The computed component.</param>
    /// <param name="bcl">The framework component.</param>
    /// <param name="label">A label identifying the case on failure.</param>
    private static void AssertComponentBits(double mine, double bcl, string label)
    {
        if (double.IsNaN(mine) && double.IsNaN(bcl))
            return;

        Assert.AreEqual(
            BitConverter.DoubleToInt64Bits(bcl),
            BitConverter.DoubleToInt64Bits(mine),
            $"{label}: mine={mine} bcl={bcl}");
    }

    /// <summary>Asserts that a computed value matches the framework result within relative tolerance.</summary>
    /// <param name="mine">The value produced by <see cref="Complex{T}" />.</param>
    /// <param name="bcl">The framework value to match.</param>
    /// <param name="label">A label identifying the case on failure.</param>
    /// <param name="tolerance">The relative tolerance.</param>
    private static void AssertClose(Complex<double> mine, Bcl bcl, string label, double tolerance = 1e-9)
    {
        AssertComponentClose(mine.Real, bcl.Real, $"{label}.Real", tolerance);
        AssertComponentClose(mine.Imaginary, bcl.Imaginary, $"{label}.Imaginary", tolerance);
    }

    /// <summary>Asserts that two components agree within relative tolerance.</summary>
    /// <param name="mine">The computed component.</param>
    /// <param name="bcl">The framework component.</param>
    /// <param name="label">A label identifying the case on failure.</param>
    /// <param name="tolerance">The relative tolerance.</param>
    private static void AssertComponentClose(double mine, double bcl, string label, double tolerance)
    {
        if (double.IsNaN(mine) && double.IsNaN(bcl))
            return;

        if (double.IsInfinity(mine) || double.IsInfinity(bcl))
        {
            Assert.AreEqual(bcl, mine, $"{label}: mine={mine} bcl={bcl}");
            return;
        }

        double difference = Math.Abs(mine - bcl);
        double scale = Math.Max(1.0, Math.Max(Math.Abs(mine), Math.Abs(bcl)));
        Assert.IsTrue(difference <= tolerance * scale, $"{label}: mine={mine} bcl={bcl} diff={difference}");
    }
}
