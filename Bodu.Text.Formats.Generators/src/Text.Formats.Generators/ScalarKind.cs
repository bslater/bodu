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
/// <remarks>
/// The reflection binders' set is the one <c>InvariantScalarCodec.Classify</c> lists; a parity test in the generator's
/// test project fails when a scalar type it lists is not mapped here.
/// </remarks>
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
    /// A numeric value parsed and formatted with the invariant culture: a primitive number, <see cref="decimal" />,
    /// <c>nint</c>, <c>nuint</c>, <c>Int128</c>, <c>UInt128</c>, <c>Half</c> or <c>BigInteger</c>.
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

    /// <summary>
    /// A <c>DateOnly</c> value.
    /// </summary>
    DateOnly,

    /// <summary>
    /// A <c>TimeOnly</c> value, written in its round-trip form.
    /// </summary>
    TimeOnly,

    /// <summary>
    /// A <see cref="System.Uri" /> value, relative or absolute.
    /// </summary>
    Uri,
}
