// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Complex{T}.Functions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Runtime.InteropServices;

namespace Bodu.Numerics;

public readonly partial struct Complex<T>
{
    /// <summary>The magnitude above which <see cref="AsinInternal" /> switches to its overflow-avoiding branch, <c>sqrt(MaxValue) / 2</c> for the backing type.</summary>
    private static readonly T s_asinOverflowThreshold = ComputeAsinOverflowThreshold();

    /// <summary>The natural logarithm of two, cached for the <see cref="AsinInternal" /> overflow branch.</summary>
    private static readonly T s_log2 = T.Log(T.One + T.One);

    /// <summary>
    /// Returns the principal square root of a complex value.
    /// </summary>
    /// <param name="value">The value whose square root is computed.</param>
    /// <returns>
    /// The principal square root, whose real part is non-negative and whose branch cut lies along the negative real
    /// axis.
    /// </returns>
    /// <remarks>
    /// The result is evaluated with a symmetry-respecting formula (two real square roots and a division) rather than
    /// via polar coordinates, so a purely negative input yields a purely imaginary root and a purely imaginary input
    /// yields a root with equal real and imaginary parts. Very large inputs are rescaled to avoid intermediate
    /// overflow.
    /// </remarks>
    public static Complex<T> Sqrt(Complex<T> value)
    {
        T a = value.Real;
        T b = value.Imaginary;

        if (T.IsZero(b))
        {
            return a < T.Zero
                ? new Complex<T>(T.Zero, T.Sqrt(-a))
                : new Complex<T>(T.Sqrt(a), T.Zero);
        }

        // An infinite imaginary component must be handled before the core formula, which would otherwise form
        // inf / inf = NaN. The NaN guard preserves NaN inputs rather than reporting an infinity.
        if (T.IsInfinity(b) && !T.IsNaN(a))
            return new Complex<T>(T.PositiveInfinity, b);

        T two = T.One + T.One;
        T half = T.One / two;

        // The core formula evaluates sqrt((Hypot(a, b) ± a) / 2), so it overflows once Hypot(a, b) + |a| exceeds the
        // finite range even when the true root is representable. Detect that (for finite inputs) and rescale by an exact
        // power of two, compute, and scale the result back — matching the magnitude-threshold rescale that
        // System.Numerics.Complex uses. One quartering suffices because 0.25 · MaxValue keeps Hypot + |a| finite.
        bool rescale = false;
        if (T.IsFinite(a) && T.IsFinite(b) && !T.IsFinite(Hypot(a, b) + T.Abs(a)))
        {
            T quarter = half * half;
            a *= quarter;
            b *= quarter;
            rescale = true;
        }

        T x;
        T y;
        if (a >= T.Zero)
        {
            x = T.Sqrt((Hypot(a, b) + a) * half);
            y = b / (two * x);
        }
        else
        {
            y = T.Sqrt((Hypot(a, b) - a) * half);
            if (b < T.Zero)
                y = -y;
            x = b / (two * y);
        }

        if (rescale)
        {
            x *= two;
            y *= two;
        }

        return new Complex<T>(x, y);
    }

    /// <summary>
    /// Returns <c>e</c> raised to the power of a complex value.
    /// </summary>
    /// <param name="value">The exponent.</param>
    /// <returns>The value <c>e^value</c>.</returns>
    public static Complex<T> Exp(Complex<T> value)
    {
        T expReal = T.Exp(value.Real);
        return new Complex<T>(expReal * T.Cos(value.Imaginary), expReal * T.Sin(value.Imaginary));
    }

    /// <summary>
    /// Returns the natural (base <c>e</c>) logarithm of a complex value.
    /// </summary>
    /// <param name="value">The value whose logarithm is computed.</param>
    /// <returns>The principal natural logarithm of <paramref name="value" />.</returns>
    public static Complex<T> Log(Complex<T> value) =>
        new(T.Log(Abs(value)), T.Atan2(value.Imaginary, value.Real));

    /// <summary>
    /// Returns the logarithm of a complex value in the specified base.
    /// </summary>
    /// <param name="value">The value whose logarithm is computed.</param>
    /// <param name="baseValue">The base of the logarithm.</param>
    /// <returns>The principal logarithm of <paramref name="value" /> in base <paramref name="baseValue" />.</returns>
    public static Complex<T> Log(Complex<T> value, T baseValue) =>
        Log(value) / Log(new Complex<T>(baseValue, T.Zero));

    /// <summary>
    /// Returns the base-10 logarithm of a complex value.
    /// </summary>
    /// <param name="value">The value whose logarithm is computed.</param>
    /// <returns>The principal base-10 logarithm of <paramref name="value" />.</returns>
    public static Complex<T> Log10(Complex<T> value)
    {
        Complex<T> natural = Log(value);
        T ln10 = T.Log(T.CreateChecked(10));
        return new Complex<T>(natural.Real / ln10, natural.Imaginary / ln10);
    }

    /// <summary>
    /// Returns a complex value raised to a complex power.
    /// </summary>
    /// <param name="value">The base value.</param>
    /// <param name="power">The exponent.</param>
    /// <returns>The value <c>value^power</c>, using the principal branch of the complex logarithm.</returns>
    public static Complex<T> Pow(Complex<T> value, Complex<T> power)
    {
        if (power == Zero)
            return One;

        if (value == Zero)
            return Zero;

        T valueReal = value.Real;
        T valueImaginary = value.Imaginary;
        T powerReal = power.Real;
        T powerImaginary = power.Imaginary;

        T rho = Abs(value);
        T theta = T.Atan2(valueImaginary, valueReal);
        T newRho = (powerReal * theta) + (powerImaginary * T.Log(rho));

        T scale = T.Pow(rho, powerReal) * T.Exp(-powerImaginary * theta);

        return new Complex<T>(scale * T.Cos(newRho), scale * T.Sin(newRho));
    }

    /// <summary>
    /// Returns a complex value raised to a real power.
    /// </summary>
    /// <param name="value">The base value.</param>
    /// <param name="power">The real exponent.</param>
    /// <returns>The value <c>value^power</c>.</returns>
    public static Complex<T> Pow(Complex<T> value, T power) =>
        Pow(value, new Complex<T>(power, T.Zero));

    /// <summary>
    /// Returns the sine of a complex value.
    /// </summary>
    /// <param name="value">The value whose sine is computed.</param>
    /// <returns>The sine of <paramref name="value" />.</returns>
    public static Complex<T> Sin(Complex<T> value)
    {
        T p = T.Exp(value.Imaginary);
        T q = T.One / p;
        T half = T.One / (T.One + T.One);
        T sinh = (p - q) * half;
        T cosh = (p + q) * half;
        return new Complex<T>(T.Sin(value.Real) * cosh, T.Cos(value.Real) * sinh);
    }

    /// <summary>
    /// Returns the hyperbolic sine of a complex value.
    /// </summary>
    /// <param name="value">The value whose hyperbolic sine is computed.</param>
    /// <returns>The hyperbolic sine of <paramref name="value" />.</returns>
    public static Complex<T> Sinh(Complex<T> value)
    {
        Complex<T> sin = Sin(new Complex<T>(-value.Imaginary, value.Real));
        return new Complex<T>(sin.Imaginary, -sin.Real);
    }

    /// <summary>
    /// Returns the inverse sine (arcsine) of a complex value.
    /// </summary>
    /// <param name="value">The value whose inverse sine is computed.</param>
    /// <returns>
    /// The principal inverse sine of <paramref name="value" />, with branch cuts along the real axis outside
    /// <c>[-1, 1]</c>.
    /// </returns>
    /// <remarks>
    /// Evaluated with the overflow- and cancellation-avoiding algorithm of Hull, Fairgrieve, and Tang — the same scheme
    /// <see cref="System.Numerics.Complex.Asin(System.Numerics.Complex)" /> uses — so the result, including the
    /// placement of the branch cut, matches the framework value.
    /// </remarks>
    public static Complex<T> Asin(Complex<T> value)
    {
        AsinInternal(T.Abs(value.Real), T.Abs(value.Imaginary), out T b, out T bPrime, out T v);

        T u = bPrime < T.Zero ? T.Asin(b) : T.Atan(bPrime);

        if (value.Real < T.Zero)
            u = -u;
        if (value.Imaginary < T.Zero)
            v = -v;

        return new Complex<T>(u, v);
    }

    /// <summary>
    /// Returns the cosine of a complex value.
    /// </summary>
    /// <param name="value">The value whose cosine is computed.</param>
    /// <returns>The cosine of <paramref name="value" />.</returns>
    public static Complex<T> Cos(Complex<T> value)
    {
        T p = T.Exp(value.Imaginary);
        T q = T.One / p;
        T half = T.One / (T.One + T.One);
        T sinh = (p - q) * half;
        T cosh = (p + q) * half;
        return new Complex<T>(T.Cos(value.Real) * cosh, -(T.Sin(value.Real) * sinh));
    }

    /// <summary>
    /// Returns the hyperbolic cosine of a complex value.
    /// </summary>
    /// <param name="value">The value whose hyperbolic cosine is computed.</param>
    /// <returns>The hyperbolic cosine of <paramref name="value" />.</returns>
    public static Complex<T> Cosh(Complex<T> value) =>
        Cos(new Complex<T>(-value.Imaginary, value.Real));

    /// <summary>
    /// Returns the inverse cosine (arccosine) of a complex value.
    /// </summary>
    /// <param name="value">The value whose inverse cosine is computed.</param>
    /// <returns>
    /// The principal inverse cosine of <paramref name="value" />, with branch cuts along the real axis outside
    /// <c>[-1, 1]</c>.
    /// </returns>
    /// <remarks>
    /// Evaluated with the Hull–Fairgrieve–Tang algorithm shared with
    /// <see cref="System.Numerics.Complex.Acos(System.Numerics.Complex)" />, so the result, including the placement of
    /// the branch cut, matches the framework value.
    /// </remarks>
    public static Complex<T> Acos(Complex<T> value)
    {
        AsinInternal(T.Abs(value.Real), T.Abs(value.Imaginary), out T b, out T bPrime, out T v);

        T u = bPrime < T.Zero ? T.Acos(b) : T.Atan(T.One / bPrime);

        if (value.Real < T.Zero)
            u = T.Pi - u;
        if (value.Imaginary > T.Zero)
            v = -v;

        return new Complex<T>(u, v);
    }

    /// <summary>
    /// Returns the tangent of a complex value.
    /// </summary>
    /// <param name="value">The value whose tangent is computed.</param>
    /// <returns>The tangent of <paramref name="value" />.</returns>
    /// <remarks>
    /// Uses the numerically stable double-angle identity, switching to a reciprocal-scaled form for large imaginary
    /// components to avoid overflow, matching <see cref="System.Numerics.Complex.Tan(System.Numerics.Complex)" />.
    /// </remarks>
    public static Complex<T> Tan(Complex<T> value)
    {
        T two = T.One + T.One;
        T half = T.One / two;
        T four = two + two;

        T x2 = two * value.Real;
        T y2 = two * value.Imaginary;
        T p = T.Exp(y2);
        T q = T.One / p;
        T cosh = (p + q) * half;

        if (T.Abs(value.Imaginary) <= four)
        {
            T sinh = (p - q) * half;
            T denominator = T.Cos(x2) + cosh;
            return new Complex<T>(T.Sin(x2) / denominator, sinh / denominator);
        }
        else
        {
            T denominator = T.One + (T.Cos(x2) / cosh);
            return new Complex<T>(T.Sin(x2) / cosh / denominator, T.Tanh(y2) / denominator);
        }
    }

    /// <summary>
    /// Returns the hyperbolic tangent of a complex value.
    /// </summary>
    /// <param name="value">The value whose hyperbolic tangent is computed.</param>
    /// <returns>The hyperbolic tangent of <paramref name="value" />.</returns>
    public static Complex<T> Tanh(Complex<T> value)
    {
        Complex<T> tan = Tan(new Complex<T>(-value.Imaginary, value.Real));
        return new Complex<T>(tan.Imaginary, -tan.Real);
    }

    /// <summary>
    /// Returns the inverse tangent (arctangent) of a complex value.
    /// </summary>
    /// <param name="value">The value whose inverse tangent is computed.</param>
    /// <returns>
    /// The principal inverse tangent of <paramref name="value" />, with branch cuts along the imaginary axis outside
    /// <c>[-i, i]</c>.
    /// </returns>
    /// <remarks>
    /// Evaluated as <c>(i/2) · (log(1 − i·value) − log(1 + i·value))</c>, the same closed form
    /// <see cref="System.Numerics.Complex.Atan(System.Numerics.Complex)" /> uses, so the result agrees with the
    /// framework value to within floating-point tolerance.
    /// </remarks>
    public static Complex<T> Atan(Complex<T> value)
    {
        Complex<T> two = new(T.One + T.One, T.Zero);
        return ImaginaryOne / two * (Log(One - (ImaginaryOne * value)) - Log(One + (ImaginaryOne * value)));
    }

    /// <summary>
    /// Computes the shared intermediate quantities of the inverse sine and cosine using the overflow- and
    /// cancellation-avoiding algorithm of Hull, Fairgrieve, and Tang, matching the private helper behind
    /// <see cref="System.Numerics.Complex" />.
    /// </summary>
    /// <param name="x">The absolute value of the real component.</param>
    /// <param name="y">The absolute value of the imaginary component.</param>
    /// <param name="b">
    /// On return, the sine of the result's real part, or a sentinel when the atan branch is selected.
    /// </param>
    /// <param name="bPrime">
    /// On return, the branch selector: a non-negative value routes the real part through atan.
    /// </param>
    /// <param name="v">On return, the magnitude of the result's imaginary part.</param>
    private static void AsinInternal(T x, T y, out T b, out T bPrime, out T v)
    {
        T one = T.One;
        T half = one / (one + one);

        if (x > s_asinOverflowThreshold || y > s_asinOverflowThreshold)
        {
            b = -one;
            bPrime = x / y;

            T small;
            T big;
            if (x < y)
            {
                small = x;
                big = y;
            }
            else
            {
                small = y;
                big = x;
            }

            T ratio = small / big;
            v = s_log2 + T.Log(big) + (half * Log1P(ratio * ratio));
        }
        else
        {
            T r = Hypot(x + one, y);
            T s = Hypot(x - one, y);

            T a = (r + s) * half;
            b = x / a;

            if (b > T.CreateChecked(0.75))
            {
                if (x <= one)
                {
                    T amx = (((y * y) / (r + (x + one))) + (s + (one - x))) * half;
                    bPrime = x / T.Sqrt((a + x) * amx);
                }
                else
                {
                    T t = ((one / (r + (x + one))) + (one / (s + (x - one)))) * half;
                    bPrime = x / y / T.Sqrt((a + x) * t);
                }
            }
            else
            {
                bPrime = -one;
            }

            if (a < T.CreateChecked(1.5))
            {
                if (x < one)
                {
                    T t = ((one / (r + (x + one))) + (one / (s + (one - x)))) * half;
                    T am1 = y * y * t;
                    v = Log1P(am1 + (y * T.Sqrt(t * (a + one))));
                }
                else
                {
                    T am1 = (((y * y) / (r + (x + one))) + (s + (x - one))) * half;
                    v = Log1P(am1 + T.Sqrt(am1 * (a + one)));
                }
            }
            else
            {
                v = T.Log(a + T.Sqrt((a - one) * (a + one)));
            }
        }
    }

    /// <summary>
    /// Computes <c>log(1 + x)</c> without loss of accuracy for small <paramref name="x" />, matching the private helper
    /// behind <see cref="System.Numerics.Complex" />.
    /// </summary>
    /// <param name="x">A non-negative value, or NaN.</param>
    /// <returns>The natural logarithm of <c>1 + x</c>.</returns>
    private static T Log1P(T x)
    {
        T xp1 = T.One + x;
        if (xp1 == T.One)
            return x;

        if (x < T.CreateChecked(0.75))
            return x * T.Log(xp1) / (xp1 - T.One);

        return T.Log(xp1);
    }

    /// <summary>
    /// Computes the inverse-sine overflow threshold <c>sqrt(MaxValue) / 2</c> for the backing type without requiring a
    /// generic <see cref="IMinMaxValue{TSelf}" /> constraint, resolving the maximum value from a fixed matrix of the
    /// built-in IEEE-754 types so the probe stays reflection-free and NativeAOT-safe.
    /// </summary>
    /// <returns>The magnitude above which <see cref="AsinInternal" /> takes its overflow-avoiding branch.</returns>
    private static T ComputeAsinOverflowThreshold()
    {
        if (typeof(T) == typeof(double))
            return T.CreateChecked(Math.Sqrt(double.MaxValue) / 2.0);
        if (typeof(T) == typeof(float))
            return T.CreateChecked(MathF.Sqrt(float.MaxValue) / 2.0f);
        if (typeof(T) == typeof(Half))
            return T.CreateChecked(Math.Sqrt((double)Half.MaxValue) / 2.0);
        if (typeof(T) == typeof(NFloat))
            return T.CreateChecked(Math.Sqrt((double)NFloat.MaxValue) / 2.0);

        return T.CreateChecked(Math.Sqrt(double.MaxValue) / 2.0);
    }
}
