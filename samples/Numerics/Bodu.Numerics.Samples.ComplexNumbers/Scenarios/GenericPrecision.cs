// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GenericPrecision.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;
using Bodu.Numerics;

namespace Bodu.Numerics.Samples.ComplexNumbers.Scenarios;

/// <summary>
/// Demonstrates the reason <see cref="Complex{T}" /> is generic: one algorithm runs over any
/// <see cref="IFloatingPointIeee754{TSelf}" /> backing type, and the choice of type decides the precision and the
/// storage cost.
/// </summary>
public static class GenericPrecision
{
    /// <summary>
    /// Runs the same computation over <see cref="float" />, <see cref="double" />, and <see cref="Half" />, then shows
    /// parsing, formatting, and the special values.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Complex<T> - one algorithm, three precisions",
            what: "Runs the same Mandelbrot-style iteration over Complex<float>, Complex<double>, and Complex<Half> " +
                  "through a single generic method, then shows parse/format round-tripping and the special values.",
            why: "This is what the type parameter buys and what System.Numerics.Complex cannot do. A graphics or " +
                 "signal pipeline that is float end to end pays a widen-narrow on every BCL complex operation; a " +
                 "Half-backed one has no BCL option at all. Writing the iteration once against " +
                 "IFloatingPointIeee754<T> means the precision becomes a caller's decision rather than a rewrite - " +
                 "and the run below shows it is a real decision, because Half reaches a different answer on an " +
                 "input that float and double agree on.",
            expect: "float and double agree on all three points. Half does not agree on c = -2+0.007i: float and double " +
                    "escape immediately at iteration 0, while Half survives two iterations. That is not a bug in " +
                    "either - |c|^2 there is 4.00005, and Half's 11-bit mantissa cannot hold a value that close to " +
                    "4, so it rounds to exactly 4 and the 'greater than 4' test does not fire. Parse round-trips " +
                    "the formatted text exactly. Note the NaN line: Equals returns true while == returns false, " +
                    "which is deliberate and explained there.");

        // One generic method, three instantiations - the escape count is a property of the arithmetic,
        // not of the backing type, so any disagreement is a precision effect.
        Console.WriteLine("  Mandelbrot escape iterations for c, limit 100:");
        Console.WriteLine($"    c = -0.5+0.5i   float={Escape<float>(-0.5f, 0.5f),-4} double={Escape<double>(-0.5, 0.5),-4} Half={Escape<Half>((Half)(-0.5f), (Half)0.5f),-4}  (well inside the set: all three agree it never escapes)");
        Console.WriteLine($"    c = 1+1i        float={Escape<float>(1f, 1f),-4} double={Escape<double>(1.0, 1.0),-4} Half={Escape<Half>((Half)1f, (Half)1f),-4}  (well outside: all three escape on the same iteration)");
        Console.WriteLine($"    c = -2+0.007i   float={Escape<float>(-2f, 0.007f),-4} double={Escape<double>(-2.0, 0.007),-4} Half={Escape<Half>((Half)(-2f), (Half)0.007f),-4}  (|c|^2 is 4.00005; Half rounds that to exactly 4, so its escape test does not fire)");
        Console.WriteLine();

        // Formatting and parsing are culture-aware and round-trip.
        var value = new Complex<double>(1.5, -2.25);
        var text = value.ToString("G", CultureInfo.InvariantCulture);
        var parsed = Complex<double>.Parse(text, CultureInfo.InvariantCulture);
        Console.WriteLine($"  formatted               : {text}");
        Console.WriteLine($"  parsed back             : real={parsed.Real}, imaginary={parsed.Imaginary}   (round-trips exactly: {(parsed.Equals(value) ? "equal" : "NOT equal")})");

        // TryParse reports failure rather than throwing, for untrusted input.
        Console.WriteLine($"  TryParse(\"not complex\") : {Complex<double>.TryParse("not complex", CultureInfo.InvariantCulture, out _)}   (false, not an exception - the shape for parsing input you do not control)");
        Console.WriteLine();

        // Two different notions of "same", and the difference is deliberate: Equals is reflexive so a NaN can be
        // used as a dictionary key and found again, while == follows IEEE 754, where NaN equals nothing.
        var nan = Complex<double>.NaN;
        Console.WriteLine($"  Complex<double>.NaN     : IsNaN={Complex<double>.IsNaN(nan)}, Equals(itself)={nan.Equals(nan)}, ==(itself)={nan == nan}   (Equals is reflexive so NaN works as a key; == follows IEEE 754, where NaN equals nothing - the same split double itself has)");
        Console.WriteLine($"  Complex<double>.Infinity: IsInfinity={Complex<double>.IsInfinity(Complex<double>.Infinity)}, IsFinite={Complex<double>.IsFinite(Complex<double>.Infinity)}");
        Console.WriteLine($"  Zero / One / i          : {Complex<double>.Zero.Real}+{Complex<double>.Zero.Imaginary}i / {Complex<double>.One.Real}+{Complex<double>.One.Imaginary}i / {Complex<double>.ImaginaryOne.Real}+{Complex<double>.ImaginaryOne.Imaginary}i");
        Console.WriteLine();
    }

    /// <summary>
    /// Counts the iterations of <c>z = z^2 + c</c> before the orbit escapes the radius-2 disc, to a limit of 100.
    /// </summary>
    /// <typeparam name="T">The floating-point type the arithmetic runs in.</typeparam>
    /// <param name="real">The real part of <c>c</c>.</param>
    /// <param name="imaginary">The imaginary part of <c>c</c>.</param>
    /// <returns>The iteration count at which the orbit escaped, or 100 if it did not.</returns>
    /// <remarks>
    /// The method is written once against <see cref="IFloatingPointIeee754{TSelf}" />; the caller's choice of
    /// <typeparamref name="T" /> is what decides how much precision the same arithmetic runs with. The escape test
    /// compares squared magnitudes to avoid a square root per iteration.
    /// </remarks>
    private static int Escape<T>(T real, T imaginary)
        where T : IFloatingPointIeee754<T>
    {
        var c = new Complex<T>(real, imaginary);
        var z = Complex<T>.Zero;
        var limit = T.CreateChecked(4);   // radius 2, squared

        for (int i = 0; i < 100; i++)
        {
            z = (z * z) + c;

            // |z|^2 without the square root: the escape criterion only needs the comparison.
            var squared = (z.Real * z.Real) + (z.Imaginary * z.Imaginary);
            if (squared > limit)
                return i;
        }

        return 100;
    }
}
