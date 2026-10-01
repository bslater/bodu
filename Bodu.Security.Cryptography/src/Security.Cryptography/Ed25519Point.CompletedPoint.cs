// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Point.CompletedPoint.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the completed coordinates in which <see cref="Ed25519Point" />'s mixed additions and projective doublings
/// leave their results.
/// </summary>
internal readonly partial struct Ed25519Point
{
    /// <summary>
    /// Represents a point in completed coordinates ((X : Z), (Y : T)), with x = X/Z and y = Y/T, the form in which an
    /// addition or a doubling leaves its result before it is converted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Converting to extended coordinates costs four multiplications and to projective coordinates, which omit T,
    /// three. A doubling whose result is only doubled again needs the projective form alone, so it saves one.
    /// </para>
    /// <para>
    /// Each coordinate is the sum or difference of two loosely reduced values, with limbs below 2^54: valid factors of
    /// a multiplication, which is all a conversion does with them.
    /// </para>
    /// </remarks>
    internal readonly struct CompletedPoint
    {
        /// <summary>The numerator of x.</summary>
        internal readonly Curve25519FieldElement X;

        /// <summary>The numerator of y.</summary>
        internal readonly Curve25519FieldElement Y;

        /// <summary>The denominator of x.</summary>
        internal readonly Curve25519FieldElement Z;

        /// <summary>The denominator of y.</summary>
        internal readonly Curve25519FieldElement T;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompletedPoint" /> struct from explicit completed coordinates.
        /// </summary>
        /// <param name="x">The numerator of x.</param>
        /// <param name="y">The numerator of y.</param>
        /// <param name="z">The denominator of x.</param>
        /// <param name="t">The denominator of y.</param>
        internal CompletedPoint(
            in Curve25519FieldElement x,
            in Curve25519FieldElement y,
            in Curve25519FieldElement z,
            in Curve25519FieldElement t)
        {
            X = x;
            Y = y;
            Z = z;
            T = t;
        }

        /// <summary>
        /// Converts this point to extended coordinates (X·T : Y·Z : Z·T : X·Y).
        /// </summary>
        /// <returns>The same point in extended coordinates, with every limb below 2^52.</returns>
        internal Ed25519Point ToExtended() =>
            new(
                Curve25519FieldElement.Multiply(X, T),
                Curve25519FieldElement.Multiply(Y, Z),
                Curve25519FieldElement.Multiply(Z, T),
                Curve25519FieldElement.Multiply(X, Y));

        /// <summary>
        /// Converts this point to projective coordinates (X·T : Y·Z : Z·T), which omit the extended coordinate.
        /// </summary>
        /// <returns>The same point in projective coordinates, with every limb below 2^52.</returns>
        internal ProjectivePoint ToProjective() =>
            new(
                Curve25519FieldElement.Multiply(X, T),
                Curve25519FieldElement.Multiply(Y, Z),
                Curve25519FieldElement.Multiply(Z, T));
    }
}
