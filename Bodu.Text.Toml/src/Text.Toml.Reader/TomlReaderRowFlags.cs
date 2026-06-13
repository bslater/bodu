// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReaderRowFlags.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Reader;

/// <summary>
/// Structural classifications a <see cref="TomlDocumentBuilder" /> records on a table or array row while parsing, used
/// to enforce TOML's table, dotted-key, inline-table, and array-of-tables rules. These flags are meaningful only during
/// the build; consumers of the finished row store ignore them.
/// </summary>
[Flags]
internal enum TomlReaderRowFlags
{
    /// <summary>No structural classification.</summary>
    None = 0,

    /// <summary>The table was explicitly defined by a <c>[table]</c> header.</summary>
    HeaderDefined = 1,

    /// <summary>The table was created as an intermediate of a dotted key.</summary>
    Dotted = 2,

    /// <summary>The table is an inline table, closed to further extension once defined.</summary>
    Inline = 4,

    /// <summary>The table is an implicit super-table created by a header path, which a later header may promote.</summary>
    ImplicitSuper = 8,

    /// <summary>The array was created by an array-of-tables header and may have further elements appended.</summary>
    TableArray = 16,
}
