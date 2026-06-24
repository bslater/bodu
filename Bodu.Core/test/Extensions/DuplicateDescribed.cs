// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DuplicateDescribed.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// A sample enumeration whose two members share the same description, used to exercise the deterministic first-wins
/// reverse lookup of <see cref="Enums.TryParseDescription{TEnum}(string, out TEnum)" />.
/// </summary>
public enum DuplicateDescribed
{
    /// <summary>The first member declaring the shared description.</summary>
    [System.ComponentModel.Description("Shared")]
    First = 0,

    /// <summary>The second member declaring the shared description.</summary>
    [System.ComponentModel.Description("Shared")]
    Second = 1,
}
