// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Option{T}.Formatting.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public readonly partial struct Option<T>
{
    /// <summary>
    /// Returns a string representation of the option.
    /// </summary>
    /// <returns><c>"Some(value)"</c> when a value is present; otherwise <c>"None"</c>.</returns>
    public override string ToString() =>
        _hasValue ? $"Some({_value})" : "None";
}
