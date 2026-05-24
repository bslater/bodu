// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedMalformedRecordBehavior.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// Defines how the delimited-text parser reacts to characters that appear immediately after a closing quote and
/// before the field delimiter or line terminator.
/// </summary>
/// <remarks>
/// <para>
/// RFC 4180 does not permit text outside the surrounding quotes of a quoted field. Real-world sources occasionally
/// emit malformed records — for example, an unescaped quote inside a quoted field that prematurely closes the
/// field. The default <see cref="Throw" /> policy surfaces these as parse errors. <see cref="SkipRecord" /> mirrors
/// historical lenient parsers that discard the remainder of the malformed record.
/// </para>
/// </remarks>
public enum DelimitedMalformedRecordBehavior
{
    /// <summary>
    /// A character following a closing quote that is neither the delimiter nor a line terminator is treated as a
    /// structural error. The parser throws a <see cref="DelimitedFormatException" /> that identifies the line on
    /// which the malformed record was detected. This is the default behaviour.
    /// </summary>
    Throw = 0,

    /// <summary>
    /// The remainder of the malformed record is discarded silently after the closing quote, and parsing resumes
    /// on the next line. The quoted field is retained; every subsequent field on the same record is lost.
    /// </summary>
    SkipRecord = 1,
}
