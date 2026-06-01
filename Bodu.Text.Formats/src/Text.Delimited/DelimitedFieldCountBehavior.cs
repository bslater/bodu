// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedFieldCountBehavior.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// Defines how the delimited-text parser reacts to data rows whose field count differs from the header row's field
/// count.
/// </summary>
/// <remarks>
/// <para>
/// The reference field count is taken from the header row when one is present. When no header is parsed the policy is
/// not applied because there is no anchor to compare against; sources whose rows must remain rectangular in that mode
/// should be validated by the caller.
/// </para>
/// </remarks>
public enum DelimitedFieldCountBehavior
{
    /// <summary>
    /// Every non-header row must contain exactly as many fields as the header row. Rows with too few or too many fields
    /// cause the parser to throw a <see cref="DelimitedFormatException" /> identifying the offending row's line number.
    /// This is the default behaviour when <see cref="DelimitedParseOptions.HasHeader" /> is <see langword="true" />.
    /// </summary>
    Strict = 0,

    /// <summary>
    /// Rows may contain any number of fields. Each row's <see cref="DelimitedRow.Count" /> reflects the fields actually
    /// present in the source. Name-based lookup of headers missing from a particular row returns
    /// <see cref="string.Empty" />.
    /// </summary>
    Ragged = 1,
}
