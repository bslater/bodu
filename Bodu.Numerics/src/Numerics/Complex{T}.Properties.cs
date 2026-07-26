// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Complex{T}.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public readonly partial struct Complex<T>
{
    /// <summary>
    /// Gets the magnitude (absolute value) of the complex value.
    /// </summary>
    /// <value>
    /// The Euclidean distance of the value from the origin, <c>sqrt(Real² + Imaginary²)</c>, computed with overflow-
    /// and underflow-avoiding scaling.
    /// </value>
    public T Magnitude =>
        Abs(this);

    /// <summary>
    /// Gets the phase (argument) of the complex value.
    /// </summary>
    /// <value>The angle in radians from the positive real axis, in the range <c>(-π, π]</c>.</value>
    public T Phase =>
        T.Atan2(Imaginary, Real);
}
