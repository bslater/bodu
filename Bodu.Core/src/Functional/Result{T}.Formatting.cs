// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Result{T}.Formatting.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public readonly partial struct Result<T>
{
    /// <summary>
    /// Returns a string representation of the result.
    /// </summary>
    /// <returns><c>"Success(value)"</c> for success; otherwise <c>"Failure(error)"</c>.</returns>
    public override string ToString() =>
        _isSuccess ? $"Success({_value})" : $"Failure({_error})";
}
