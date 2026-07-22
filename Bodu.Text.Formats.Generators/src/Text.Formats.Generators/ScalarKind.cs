// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScalarKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats.Generators;

/// <summary>
/// Identifies the scalar conversion strategy for a mapped member, mirroring the scalar set the runtime reflection
/// binders convert with the invariant culture.
/// </summary>
internal enum ScalarKind
{
    /// <summary>
    /// The member type is not a supported scalar; the member is skipped with a diagnostic.
    /// </summary>
    Unsupported,

    /// <summary>
    /// A <see cref="string" /> value, transferred verbatim.
    /// </summary>
    String,

    /// <summary>
    /// A <see cref="bool" /> value, written as <c>true</c>/<c>false</c>.
    /// </summary>
    Boolean,

    /// <summary>
    /// A <see cref="char" /> value.
    /// </summary>
    Char,

    /// <summary>
    /// A numeric value parsed and formatted with the invariant culture.
    /// </summary>
    Number,

    /// <summary>
    /// A <see cref="Guid" /> value.
    /// </summary>
    Guid,

    /// <summary>
    /// A <see cref="DateTime" /> value parsed with round-trip kind semantics.
    /// </summary>
    DateTime,

    /// <summary>
    /// A <see cref="DateTimeOffset" /> value parsed with round-trip kind semantics.
    /// </summary>
    DateTimeOffset,

    /// <summary>
    /// A <see cref="TimeSpan" /> value.
    /// </summary>
    TimeSpan,

    /// <summary>
    /// An enum value parsed case-insensitively by name or numeric value.
    /// </summary>
    Enum,
}
