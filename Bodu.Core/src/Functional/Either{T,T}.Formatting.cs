// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Either{T,T}.Formatting.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public readonly partial struct Either<TLeft, TRight>
{
    /// <summary>
    /// Returns a string representation of the either.
    /// </summary>
    /// <returns>
    /// <c>"Left(value)"</c> when the left side is active, <c>"Right(value)"</c> when the right side is active, or
    /// <c>"Either()"</c> for an uninitialized instance.
    /// </returns>
    public override string ToString() =>
        _state switch
        {
            StateLeft => $"Left({_left})",
            StateRight => $"Right({_right})",
            _ => "Either()",
        };
}
