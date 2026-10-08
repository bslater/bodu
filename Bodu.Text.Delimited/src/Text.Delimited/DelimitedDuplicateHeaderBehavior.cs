// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedDuplicateHeaderBehavior.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// Specifies how a delimited reader resolves a header row that contains the same column name more than once.
/// </summary>
/// <remarks>
/// The policy is a reader option, so it applies to <see cref="Reader.Utf8DelimitedReader" /> and to the document models
/// parsed through it. <see cref="DelimitedSerializer" /> takes no reader policies: in header mode it always throws
/// <see cref="DelimitedFormatException" /> for a duplicate header name.
/// </remarks>
public enum DelimitedDuplicateHeaderBehavior
{
    /// <summary>
    /// A duplicate header name throws <see cref="DelimitedFormatException" />.
    /// </summary>
    Throw = 0,

    /// <summary>
    /// The reader reports every column under its own name, so a duplicated name appears once for each column that has
    /// it, and <see cref="Document.DelimitedElement.GetProperty(string)" /> finds the first of them.
    /// </summary>
    TakeFirst,

    /// <summary>
    /// The reader reports the last column with a duplicated name under that name and the earlier ones under an empty
    /// name, so <see cref="Document.DelimitedElement.GetProperty(string)" /> finds the last of them.
    /// </summary>
    TakeLast,
}
