// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PolarAndFunctions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Numerics;
using SysComplex = System.Numerics.Complex;

namespace Bodu.Numerics.Samples.ComplexNumbers.Scenarios;

/// <summary>
/// Demonstrates the polar form of <see cref="Complex{T}" /> and the transcendental functions defined over it, each
/// checked against <see cref="SysComplex" />.
/// </summary>
public static class PolarAndFunctions
{
    /// <summary>
    /// Converts between rectangular and polar form, then exercises the root, exponential, logarithm, power, and
    /// trigonometric functions.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Complex<T> - polar form and transcendental functions",
            what: "Round-trips a value through FromPolarCoordinates, takes the square root of a negative real, walks " +
                  "Euler's identity through Exp, and runs Log, Pow, and the trig functions - printing the BCL's " +
                  "answer for each.",
            why: "These are the operations that motivate complex arithmetic in the first place. Sqrt(-1) has no real " +
                 "answer, so a real-only pipeline must either throw or return NaN; over the complex plane it is " +
                 "simply i. Exp turns rotation into multiplication, which is the whole basis of signal processing " +
                 "and the FFT. Pow with a fractional exponent needs a branch cut decision, and Log needs a principal " +
                 "value - this type takes the same conventions as the BCL rather than inventing its own, which is " +
                 "what makes results portable between the two.",
            expect: "Sqrt(-1) is exactly 0+1i. Exp(i*pi) is -1 to within floating-point noise - the imaginary part " +
                    "prints as a tiny value near 1.2e-16, not a clean zero, because pi itself is not exact in " +
                    "double. Every line is marked 'match' against the BCL, including that noise, because both " +
                    "compute it the same way.");

        // Polar form: a magnitude and an angle. The round trip should return where it started.
        var polar = Complex<double>.FromPolarCoordinates(2.0, Math.PI / 4.0);
        var bclPolar = SysComplex.FromPolarCoordinates(2.0, Math.PI / 4.0);
        Report("polar(r=2, theta=pi/4)", polar, bclPolar, "sqrt(2) in both parts - 45 degrees out at radius 2");
        Console.WriteLine($"  back to polar           : r={polar.Magnitude:0.######}, theta={polar.Phase:0.######}   (the round trip returns the magnitude and angle it was built from)");
        Console.WriteLine();

        // The square root of a negative real - the case that has no answer in the reals.
        Report("Sqrt(-1)", Complex<double>.Sqrt(new Complex<double>(-1.0, 0.0)), SysComplex.Sqrt(new SysComplex(-1.0, 0.0)),
            "no real answer exists; over the complex plane it is i");
        Report("Sqrt(3+4i)", Complex<double>.Sqrt(new Complex<double>(3.0, 4.0)), SysComplex.Sqrt(new SysComplex(3.0, 4.0)),
            "2+1i, and squaring it returns 3+4i");
        Console.WriteLine();

        // Euler: e^(i*pi) = -1. The imaginary part shows the cost of pi not being exact in double.
        var euler = Complex<double>.Exp(new Complex<double>(0.0, Math.PI));
        Report("Exp(i*pi)", euler, SysComplex.Exp(new SysComplex(0.0, Math.PI)),
            "Euler's identity: -1, with residue because pi is inexact");
        Report("Log(1+0i)", Complex<double>.Log(Complex<double>.One), SysComplex.Log(SysComplex.One), "log of 1 is 0 on both axes");
        Report("Log(-1+0i)", Complex<double>.Log(new Complex<double>(-1.0, 0.0)), SysComplex.Log(new SysComplex(-1.0, 0.0)),
            "the principal value: 0 + pi*i");
        Console.WriteLine();

        // Powers, including the one that surprises people: i to the i is real.
        Report("(1+1i)^2", Complex<double>.Pow(new Complex<double>(1.0, 1.0), 2.0), SysComplex.Pow(new SysComplex(1.0, 1.0), 2.0),
            "0+2i, since (1+i)^2 = 2i");
        Report("i^i", Complex<double>.Pow(Complex<double>.ImaginaryOne, Complex<double>.ImaginaryOne),
            SysComplex.Pow(SysComplex.ImaginaryOne, SysComplex.ImaginaryOne), "a real result, e^(-pi/2), from two imaginary operands");
        Console.WriteLine();

        // Trig over the complex plane, where sin is no longer bounded by 1.
        Report("Sin(1+1i)", Complex<double>.Sin(new Complex<double>(1.0, 1.0)), SysComplex.Sin(new SysComplex(1.0, 1.0)),
            "complex sin is unbounded - the real-valued |sin| <= 1 rule does not hold here");
        Report("Cos(1+1i)", Complex<double>.Cos(new Complex<double>(1.0, 1.0)), SysComplex.Cos(new SysComplex(1.0, 1.0)), "likewise unbounded");
        Console.WriteLine();
    }

    /// <summary>
    /// Prints one function's Bodu result, the BCL's result, and whether they agree.
    /// </summary>
    /// <param name="label">The function call being shown.</param>
    /// <param name="actual">The <see cref="Complex{T}" /> result.</param>
    /// <param name="expected">The <see cref="SysComplex" /> result for the same call.</param>
    /// <param name="note">A short note on what the call illustrates.</param>
    private static void Report(string label, Complex<double> actual, SysComplex expected, string note)
    {
        var mine = Format(actual.Real, actual.Imaginary);
        var theirs = Format(expected.Real, expected.Imaginary);
        var verdict = mine == theirs ? "match" : "DIFFERS";

        Console.WriteLine($"  {label,-23} : {mine,-26} BCL {theirs,-26} {verdict}   ({note})");
    }

    /// <summary>
    /// Formats a real/imaginary pair, keeping enough digits that floating-point residue stays visible.
    /// </summary>
    /// <param name="real">The real part.</param>
    /// <param name="imaginary">The imaginary part.</param>
    /// <returns>The formatted value.</returns>
    private static string Format(double real, double imaginary) =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{real:G6}{(imaginary < 0 ? "-" : "+")}{Math.Abs(imaginary):G6}i");
}
