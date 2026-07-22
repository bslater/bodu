// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedFieldCountBehavior.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// Specifies how a delimited reader treats a data record whose field count differs from the header's.
/// </summary>
public enum DelimitedFieldCountBehavior
{
    /// <summary>
    /// Every record must have the same field count as the header; a mismatch is an error.
    /// </summary>
    Strict = 0,

    /// <summary>
    /// Records may have any field count; short records expose fewer fields and long records expose more.
    /// </summary>
    Ragged,
}
