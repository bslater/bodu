// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Complex{T}.Conversions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public readonly partial struct Complex<T>
{
    /// <summary>
    /// Converts a real value to a <see cref="Complex{T}" /> by placing it on the real axis.
    /// </summary>
    /// <param name="value">The real value to embed.</param>
    /// <returns>The complex value <c>value + 0i</c>.</returns>
    /// <remarks>
    /// This is the generic counterpart of the implicit conversions <see cref="System.Numerics.Complex" /> defines from
    /// each built-in real type. A type parameter cannot carry a conversion from every such type, so the single
    /// conversion from <typeparamref name="T" /> is provided; convert other numeric sources with <see cref="Create" />
    /// or the <see cref="System.Numerics.INumberBase{TSelf}" /> conversion helpers first.
    /// </remarks>
    public static implicit operator Complex<T>(T value) =>
        new(value, T.Zero);
}
