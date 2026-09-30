// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Point.ProjectiveNielsPoint.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the projective Niels form in which <see cref="Ed25519Point" /> holds the multiples of a variable point.
/// </summary>
internal readonly partial struct Ed25519Point
{
    /// <summary>
    /// Represents a point in projective Niels form (Y + X, Y − X, Z, 2d·T), the form in which verification holds the
    /// odd multiples of the public point.
    /// </summary>
    /// <remarks>
    /// An addition of a point in this form to an extended point needs four multiplications, where the unified addition
    /// of <see cref="Ed25519Point.Add(in Ed25519Point)" /> needs nine. The form keeps Z, so it costs no inversion to
    /// form, only the one multiplication by 2d.
    /// </remarks>
    internal readonly struct ProjectiveNielsPoint
    {
        /// <summary>The sum Y + X of the projective coordinates.</summary>
        internal readonly Curve25519FieldElement YPlusX;

        /// <summary>The difference Y − X of the projective coordinates.</summary>
        internal readonly Curve25519FieldElement YMinusX;

        /// <summary>The Z coordinate.</summary>
        internal readonly Curve25519FieldElement Z;

        /// <summary>The product 2d·T of the extended coordinate and the doubled curve constant.</summary>
        internal readonly Curve25519FieldElement T2d;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectiveNielsPoint" /> struct from explicit Niels
        /// coordinates.
        /// </summary>
        /// <param name="yPlusX">The sum Y + X.</param>
        /// <param name="yMinusX">The difference Y − X.</param>
        /// <param name="z">The Z coordinate.</param>
        /// <param name="t2d">The product 2d·T.</param>
        internal ProjectiveNielsPoint(
            in Curve25519FieldElement yPlusX,
            in Curve25519FieldElement yMinusX,
            in Curve25519FieldElement z,
            in Curve25519FieldElement t2d)
        {
            YPlusX = yPlusX;
            YMinusX = yMinusX;
            Z = z;
            T2d = t2d;
        }
    }
}
