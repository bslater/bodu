// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComplexBasics.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Numerics;
using SysComplex = System.Numerics.Complex;

namespace Bodu.Numerics.Samples.ComplexNumbers.Scenarios;

/// <summary>
/// Demonstrates the arithmetic surface of <see cref="Complex{T}" /> and checks each result against the BCL's
/// <see cref="SysComplex" />, which is the same algebra fixed to <see cref="double" />.
/// </summary>
public static class ComplexBasics
{
    /// <summary>
    /// Adds, multiplies, divides, and conjugates complex values, printing the BCL's answer beside each one.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Complex<T> - arithmetic, conjugate, magnitude",
            what: "Builds two complex values, runs the four arithmetic operators plus Conjugate and Reciprocal over " +
                  "them, and prints System.Numerics.Complex's answer for the same operation alongside each result.",
            why: "Complex multiplication is not componentwise - (a+bi)(c+di) = (ac-bd) + (ad+bc)i - so a hand-rolled " +
                 "pair of doubles gets it wrong in a way that is easy to miss and hard to debug. The reason this type " +
                 "exists rather than the BCL's is the type parameter: System.Numerics.Complex is double only, so a " +
                 "float pipeline pays a widening on every operation and a decimal-backed or higher-precision one " +
                 "cannot use it at all. Pinning against the BCL is how the sample shows the generic version did not " +
                 "trade correctness for that flexibility.",
            expect: "Every Bodu line matches the BCL line to the last printed digit, and each is marked 'match'. " +
                    "(2+3i)(4-1i) is 11+10i, not the componentwise 8-3i. Multiplying 2+3i by its own conjugate " +
                    "2-3i gives the real 13 with a zero imaginary part, which is |2+3i|^2 - the identity that makes " +
                    "the conjugate the tool for removing i from a denominator.");

        var left = new Complex<double>(2.0, 3.0);
        var right = new Complex<double>(4.0, -1.0);

        var bclLeft = new SysComplex(2.0, 3.0);
        var bclRight = new SysComplex(4.0, -1.0);

        Report("(2+3i) + (4-1i)", left + right, bclLeft + bclRight, "componentwise, so the easy case");
        Report("(2+3i) - (4-1i)", left - right, bclLeft - bclRight, "also componentwise");

        // The interesting one: i*i = -1 folds the imaginary product back into the real part.
        Report("(2+3i) * (4-1i)", left * right, bclLeft * bclRight, "NOT componentwise: (ac-bd)+(ad+bc)i = 11+10i");
        Report("(2+3i) / (4-1i)", left / right, bclLeft / bclRight, "division multiplies by the conjugate of the divisor");

        Console.WriteLine();

        // The conjugate flips the sign of the imaginary part, and z * conj(z) is always real.
        var conjugate = Complex<double>.Conjugate(left);
        Report("conj(2+3i)", conjugate, SysComplex.Conjugate(bclLeft), "the imaginary sign flips");
        Report("(2+3i) * conj(2+3i)", left * conjugate, bclLeft * SysComplex.Conjugate(bclLeft),
            "real 13 = |2+3i|^2, which is why the conjugate clears i from a denominator");
        Report("1 / (2+3i)", Complex<double>.Reciprocal(left), SysComplex.Reciprocal(bclLeft), "conj(z) / |z|^2");

        Console.WriteLine();

        // Magnitude is the Euclidean length; Phase is the angle from the positive real axis.
        Console.WriteLine($"  |3+4i| magnitude        : {new Complex<double>(3.0, 4.0).Magnitude,-22} BCL {new SysComplex(3.0, 4.0).Magnitude}   (the 3-4-5 triangle, so exactly 5)");
        Console.WriteLine($"  arg(0+1i) phase         : {Complex<double>.ImaginaryOne.Phase,-22} BCL {SysComplex.ImaginaryOne.Phase}   (straight up the imaginary axis: pi/2)");
        Console.WriteLine();
    }

    /// <summary>
    /// Prints one operation's Bodu result, the BCL's result for the same operation, and whether they agree.
    /// </summary>
    /// <param name="label">The operation being shown.</param>
    /// <param name="actual">The <see cref="Complex{T}" /> result.</param>
    /// <param name="expected">The <see cref="SysComplex" /> result for the same operation.</param>
    /// <param name="note">A short note on what the operation illustrates.</param>
    private static void Report(string label, Complex<double> actual, SysComplex expected, string note)
    {
        // Compare the printed form rather than the bits: both types compute in double, so an exact
        // comparison would be fair, but printing is what a reader can check by eye.
        var mine = Format(actual.Real, actual.Imaginary);
        var theirs = Format(expected.Real, expected.Imaginary);
        var verdict = mine == theirs ? "match" : "DIFFERS";

        Console.WriteLine($"  {label,-23} : {mine,-22} BCL {theirs,-22} {verdict}   ({note})");
    }

    /// <summary>
    /// Formats a real/imaginary pair as <c>a+bi</c>, with the sign carried by the imaginary term.
    /// </summary>
    /// <param name="real">The real part.</param>
    /// <param name="imaginary">The imaginary part.</param>
    /// <returns>The formatted value.</returns>
    private static string Format(double real, double imaginary) =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{real:0.######}{(imaginary < 0 ? "-" : "+")}{Math.Abs(imaginary):0.######}i");
}
