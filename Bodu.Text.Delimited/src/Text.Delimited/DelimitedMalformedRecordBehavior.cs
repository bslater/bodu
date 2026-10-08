// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedMalformedRecordBehavior.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// Specifies how a delimited reader responds to a malformed record: one whose field count violates the configured
/// field-count policy, or one in which text follows a closing quote.
/// </summary>
/// <remarks>
/// After a closing quote only the delimiter, a line break or the end of the input may follow, with spaces and tabs
/// before them when <see cref="Reader.DelimitedReaderOptions.TrimFields" /> is set. An unterminated quoted field is not
/// covered by this policy: it always throws.
/// </remarks>
public enum DelimitedMalformedRecordBehavior
{
    /// <summary>
    /// A malformed record throws <see cref="DelimitedFormatException" />: at the record's start for a field-count
    /// violation, and at the offending byte for text after a closing quote.
    /// </summary>
    Throw = 0,

    /// <summary>
    /// A malformed record is silently skipped whole, and reading continues with the next line: the line after the
    /// record for a field-count violation, or after the offending text for text after a closing quote.
    /// </summary>
    SkipRecord,
}
