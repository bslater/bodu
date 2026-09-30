// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Point.ProjectivePoint.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the projective coordinates in which <see cref="Ed25519Point" /> doubles without the extended coordinate.
/// </summary>
internal readonly partial struct Ed25519Point
{
    /// <summary>
    /// Represents a point in projective coordinates (X : Y : Z), with x = X/Z and y = Y/Z: the extended coordinates
    /// without T, which a doubling neither reads nor needs to produce.
    /// </summary>
    internal readonly struct ProjectivePoint
    {
        /// <summary>The X coordinate.</summary>
        internal readonly Curve25519FieldElement X;

        /// <summary>The Y coordinate.</summary>
        internal readonly Curve25519FieldElement Y;

        /// <summary>The Z coordinate.</summary>
        internal readonly Curve25519FieldElement Z;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectivePoint" /> struct from explicit projective
        /// coordinates.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        /// <param name="z">The Z coordinate.</param>
        internal ProjectivePoint(in Curve25519FieldElement x, in Curve25519FieldElement y, in Curve25519FieldElement z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Gets the identity element (0, 1) of the curve group in projective coordinates.
        /// </summary>
        /// <value>The neutral point (0 : 1 : 1).</value>
        internal static ProjectivePoint Identity =>
            new(Curve25519FieldElement.Zero, Curve25519FieldElement.One, Curve25519FieldElement.One);

        /// <summary>
        /// Doubles this point, leaving the result in completed coordinates.
        /// </summary>
        /// <returns>The point added to itself.</returns>
        /// <remarks>
        /// <para>
        /// Four squarings, the doubling formula of Hisil, Wong, Carter, and Dawson (dbl-2008-hwcd) for a = −1 before
        /// its final multiplications, which the conversion out of completed coordinates performs: x = E/G and y = H/F,
        /// where E = (X + Y)² − X² − Y², G = Y² − X², F = G − 2Z², and H = −(X² + Y²). This forms −H and −F as the
        /// numerator and denominator of y, which leaves y unchanged. Like <see cref="Ed25519Point.Double()" />, it is
        /// complete.
        /// </para>
        /// <para>
        /// The coordinates must have limbs below 2^53, as every conversion out of completed coordinates leaves them.
        /// </para>
        /// </remarks>
        internal CompletedPoint Double()
        {
            var xx = Curve25519FieldElement.Square(X);
            var yy = Curve25519FieldElement.Square(Y);
            var zz = Curve25519FieldElement.Square(Z);
            var sum = Curve25519FieldElement.Square(Curve25519FieldElement.Add(X, Y));

            var negatedH = Curve25519FieldElement.Add(yy, xx);

            // G is re-reduced because it is subtracted in turn, and Subtract requires tight operands.
            var g = Curve25519FieldElement.Reduce(Curve25519FieldElement.Subtract(yy, xx));

            return new CompletedPoint(
                Curve25519FieldElement.Subtract(sum, negatedH),
                negatedH,
                g,
                Curve25519FieldElement.Subtract(Curve25519FieldElement.Add(zz, zz), g));
        }
    }
}
