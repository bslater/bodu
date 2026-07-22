// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedMalformedRecordBehavior.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// Specifies how a delimited reader responds to a record that violates the configured field-count policy.
/// </summary>
public enum DelimitedMalformedRecordBehavior
{
    /// <summary>
    /// A malformed record throws <see cref="DelimitedFormatException" />.
    /// </summary>
    Throw = 0,

    /// <summary>
    /// A malformed record is silently skipped.
    /// </summary>
    SkipRecord,
}
