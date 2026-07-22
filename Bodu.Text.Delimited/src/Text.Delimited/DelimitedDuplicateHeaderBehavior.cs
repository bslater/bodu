// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedDuplicateHeaderBehavior.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// Specifies how a delimited reader resolves a header row that contains the same column name more than once.
/// </summary>
public enum DelimitedDuplicateHeaderBehavior
{
    /// <summary>
    /// A duplicate header name throws <see cref="DelimitedFormatException" />.
    /// </summary>
    Throw = 0,

    /// <summary>
    /// The first occurrence of a duplicated header name wins; later columns with that name are unnamed.
    /// </summary>
    TakeFirst,

    /// <summary>
    /// The last occurrence of a duplicated header name wins.
    /// </summary>
    TakeLast,
}
