// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedDuplicateHeaderBehavior.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// Defines how the delimited-text parser resolves duplicate header names when constructing the column name-to-index map
/// for a document with a header row.
/// </summary>
/// <remarks>
/// <para>
/// Duplicate header names are ambiguous for name-based field lookup, because more than one column would compete to own
/// the same name. The default <see cref="Throw" /> policy treats this as a structural error and surfaces the problem to
/// the caller. The remaining values exist for sources that legitimately repeat column names — for example, exports from
/// legacy systems — and let the caller choose which occurrence wins or accept the input while disabling name-based
/// lookup for affected columns.
/// </para>
/// </remarks>
public enum DelimitedDuplicateHeaderBehavior
{
    /// <summary>
    /// Duplicate header names are not permitted. The parser throws a <see cref="DelimitedFormatException" /> when a
    /// header name repeats. This is the default behaviour.
    /// </summary>
    Throw = 0,

    /// <summary>
    /// The first occurrence of a duplicated header name wins. Subsequent occurrences are accepted but do not update the
    /// name-to-index map; access by name returns the field at the first occurrence's index.
    /// </summary>
    FirstWins = 1,

    /// <summary>
    /// The last occurrence of a duplicated header name wins. Earlier occurrences are silently overridden in the
    /// name-to-index map; access by name returns the field at the last occurrence's index.
    /// </summary>
    LastWins = 2,

    /// <summary>
    /// Duplicate header names are tolerated but excluded from the name-to-index map. Access by name throws for
    /// duplicated headers; access by ordinal index remains available for every column.
    /// </summary>
    AllowDuplicates = 3,
}
