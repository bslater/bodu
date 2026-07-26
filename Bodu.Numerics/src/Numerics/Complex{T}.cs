// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Complex{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Numerics;

namespace Bodu.Numerics;

/// <summary>
/// Represents an immutable complex number — a value with a real and an imaginary component — backed by an arbitrary
/// <see cref="IFloatingPointIeee754{TSelf}" /> component type.
/// </summary>
/// <typeparam name="T">The floating-point type used to store the real and imaginary components.</typeparam>
/// <remarks>
/// <para>
/// A <see cref="Complex{T}" /> generalizes the <see cref="System.Numerics.Complex" /> value type, which is fixed to
/// <see cref="double" /> components, to any IEEE 754 floating-point type. Choose <see cref="float" /> or
/// <see cref="Half" /> for compact storage, <see cref="double" /> for the widest hardware-accelerated range, or another
/// conforming type as required.
/// </para>
/// <para>
/// The arithmetic mirrors <see cref="System.Numerics.Complex" /> exactly, including its handling of the non-finite
/// values <see cref="IFloatingPointIeee754{TSelf}.NaN" /> and the infinities: multiplication uses the direct
/// four-multiply form without infinity recovery, division uses Smith's algorithm, and <see cref="Reciprocal" /> of zero
/// returns <see cref="Zero" /> rather than an infinity.
/// </para>
/// <para>
/// Because a type parameter cannot carry an implicit conversion from each built-in real type the way
/// <see cref="System.Numerics.Complex" /> does, only the implicit conversion from <typeparamref name="T" /> (embedding
/// a real value on the real axis) is provided; use <see cref="Create" />, the
/// <see cref="System.Numerics.INumberBase{TSelf}" /> conversion helpers, or <see cref="FromPolarCoordinates" /> for
/// other sources.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Construct from components and from polar coordinates.
/// Complex<double> a = new Complex<double>(3.0, 4.0);      // 3 + 4i
/// Complex<double> b = Complex<double>.FromPolarCoordinates(5.0, 0.0);
///
/// // Arithmetic through the operators.
/// Complex<double> sum = a + b;                            // 8 + 4i
/// double magnitude = a.Magnitude;                         // 5
///
/// // Parse and format round-trip through the "<real; imaginary>" form.
/// Complex<double> parsed = Complex<double>.Parse("<1; 2>", null);
/// string text = parsed.ToString();                        // "<1; 2>"
///]]>
/// </code>
/// </example>
[DebuggerDisplay("{ToString(),nq}")]
public readonly partial struct Complex<T>
    where T : IFloatingPointIeee754<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Complex{T}" /> struct with the specified real and imaginary
    /// components.
    /// </summary>
    /// <param name="real">The real component.</param>
    /// <param name="imaginary">The imaginary component.</param>
    public Complex(T real, T imaginary)
    {
        Real = real;
        Imaginary = imaginary;
    }

    /// <summary>
    /// Gets a <see cref="Complex{T}" /> representing the value zero.
    /// </summary>
    /// <value>The complex value <c>0 + 0i</c>, which is also <c>default(Complex&lt;T&gt;)</c>.</value>
    public static Complex<T> Zero =>
        new(T.Zero, T.Zero);

    /// <summary>
    /// Gets a <see cref="Complex{T}" /> representing the real unit one.
    /// </summary>
    /// <value>The complex value <c>1 + 0i</c>.</value>
    public static Complex<T> One =>
        new(T.One, T.Zero);

    /// <summary>
    /// Gets a <see cref="Complex{T}" /> representing the imaginary unit.
    /// </summary>
    /// <value>The complex value <c>0 + 1i</c>.</value>
    public static Complex<T> ImaginaryOne =>
        new(T.Zero, T.One);

    /// <summary>
    /// Gets a <see cref="Complex{T}" /> whose real and imaginary components are both
    /// <see cref="IFloatingPointIeee754{TSelf}.NaN" />.
    /// </summary>
    /// <value>The complex value <c>NaN + NaN·i</c>.</value>
    public static Complex<T> NaN =>
        new(T.NaN, T.NaN);

    /// <summary>
    /// Gets a <see cref="Complex{T}" /> whose real and imaginary components are both
    /// <see cref="IFloatingPointIeee754{TSelf}.PositiveInfinity" />.
    /// </summary>
    /// <value>The complex value <c>∞ + ∞·i</c>.</value>
    public static Complex<T> Infinity =>
        new(T.PositiveInfinity, T.PositiveInfinity);

    /// <summary>
    /// Gets the real component of the complex value.
    /// </summary>
    /// <value>The real component.</value>
    public T Real { get; }

    /// <summary>
    /// Gets the imaginary component of the complex value.
    /// </summary>
    /// <value>The imaginary component.</value>
    public T Imaginary { get; }

    /// <summary>
    /// Creates a <see cref="Complex{T}" /> from the specified real and imaginary components.
    /// </summary>
    /// <param name="real">The real component.</param>
    /// <param name="imaginary">The imaginary component.</param>
    /// <returns>A complex value with the specified components.</returns>
    public static Complex<T> Create(T real, T imaginary) =>
        new(real, imaginary);

    /// <summary>
    /// Creates a <see cref="Complex{T}" /> from a magnitude and phase expressed in polar coordinates.
    /// </summary>
    /// <param name="magnitude">The distance of the value from the origin.</param>
    /// <param name="phase">The angle, in radians, measured from the positive real axis.</param>
    /// <returns>The complex value <c>magnitude · (cos(phase) + i·sin(phase))</c>.</returns>
    public static Complex<T> FromPolarCoordinates(T magnitude, T phase) =>
        new(magnitude * T.Cos(phase), magnitude * T.Sin(phase));

    /// <summary>
    /// Deconstructs the complex value into its real and imaginary components.
    /// </summary>
    /// <param name="real">When this method returns, contains the real component.</param>
    /// <param name="imaginary">When this method returns, contains the imaginary component.</param>
    public void Deconstruct(out T real, out T imaginary)
    {
        real = Real;
        imaginary = Imaginary;
    }
}
