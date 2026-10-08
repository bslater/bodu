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
/// <para>
/// Under <see cref="TakeFirst" /> and <see cref="TakeLast" /> one column with a duplicated name keeps it, and the
/// reader reports every other column with the name under an empty name, both as the record's property names and in
/// <see cref="Reader.Utf8DelimitedReader.Headers" />. A lookup by name therefore finds the winning column, through
/// <see cref="Document.DelimitedElement.GetProperty(string)" /> and in the records of
/// <see cref="Nodes.DelimitedNode.Parse(ReadOnlySpan{byte}, Reader.DelimitedReaderOptions)" /> alike. A
/// <see cref="Nodes.DelimitedObject" /> holds one field per name, so the columns reported under an empty name share one
/// field there, which keeps the last of their values.
/// </para>
/// <para>
/// The policy is a reader option, so it applies to <see cref="Reader.Utf8DelimitedReader" /> and to the document models
/// parsed through it. <see cref="DelimitedSerializer" /> takes no reader policies: in header mode it always throws
/// <see cref="DelimitedFormatException" /> for a duplicate header name.
/// </para>
/// </remarks>
public enum DelimitedDuplicateHeaderBehavior
{
    /// <summary>
    /// A duplicate header name throws <see cref="DelimitedFormatException" />.
    /// </summary>
    Throw = 0,

    /// <summary>
    /// The first column with a duplicated name keeps it, and the later columns with it are reported under an empty
    /// name.
    /// </summary>
    TakeFirst,

    /// <summary>
    /// The last column with a duplicated name keeps it, and the earlier columns with it are reported under an empty
    /// name.
    /// </summary>
    TakeLast,
}
