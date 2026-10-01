// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Point.AffineNielsPoint.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the affine Niels form in which <see cref="Ed25519Point" />'s precomputed multiples of the base point are
/// held.
/// </summary>
internal readonly partial struct Ed25519Point
{
    /// <summary>
    /// Represents a point in affine Niels form (y + x, y − x, 2d·x·y), the form in which the precomputed multiples of
    /// the base point are held.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An addition of a point in this form to an extended point needs three multiplications, where the unified addition
    /// of <see cref="Ed25519Point.Add(in Ed25519Point)" /> needs nine: the affine coordinates make its Z one, and the
    /// sum, difference and product with 2d are formed once, when the table is built.
    /// </para>
    /// <para>
    /// The negation of (y + x, y − x, 2d·x·y) is (y − x, y + x, −2d·x·y), so a table of positive multiples serves
    /// signed digits through a conditional swap and a conditional negation.
    /// </para>
    /// </remarks>
    internal readonly struct AffineNielsPoint
    {
        /// <summary>The sum y + x of the affine coordinates.</summary>
        internal readonly Curve25519FieldElement YPlusX;

        /// <summary>The difference y − x of the affine coordinates.</summary>
        internal readonly Curve25519FieldElement YMinusX;

        /// <summary>The product 2d·x·y of the affine coordinates and the doubled curve constant.</summary>
        internal readonly Curve25519FieldElement XY2d;

        /// <summary>
        /// Initializes a new instance of the <see cref="AffineNielsPoint" /> struct from explicit Niels coordinates.
        /// </summary>
        /// <param name="yPlusX">The sum y + x.</param>
        /// <param name="yMinusX">The difference y − x.</param>
        /// <param name="xy2d">The product 2d·x·y.</param>
        internal AffineNielsPoint(
            in Curve25519FieldElement yPlusX,
            in Curve25519FieldElement yMinusX,
            in Curve25519FieldElement xy2d)
        {
            YPlusX = yPlusX;
            YMinusX = yMinusX;
            XY2d = xy2d;
        }

        /// <summary>
        /// Gets the identity element (0, 1) of the curve group in affine Niels form.
        /// </summary>
        /// <value>The neutral point (1, 1, 0).</value>
        internal static AffineNielsPoint Identity =>
            new(Curve25519FieldElement.One, Curve25519FieldElement.One, Curve25519FieldElement.Zero);
    }
}
