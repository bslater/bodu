// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DescribedFlags.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// A sample <see cref="FlagsAttribute" /> enumeration used to exercise description resolution for single flags, a
/// declared composite, and an undeclared composite combination.
/// </summary>
[Flags]
public enum DescribedFlags
{
    /// <summary>No flags.</summary>
    None = 0,

    /// <summary>A described single flag.</summary>
    [System.ComponentModel.Description("Can Read")]
    Read = 1,

    /// <summary>A described single flag.</summary>
    [System.ComponentModel.Description("Can Write")]
    Write = 2,

    /// <summary>A described single flag.</summary>
    [System.ComponentModel.Description("Can Execute")]
    Execute = 4,

    /// <summary>A described composite flag declared as its own field.</summary>
    [System.ComponentModel.Description("Everything")]
    All = Read | Write | Execute,
}
