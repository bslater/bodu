// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Elf64Tests.Elf64Variant.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Identifies the ELF-64 seed configurations exercised by <see cref="Elf64Tests" />: the unseeded default,
/// a seed of 31, and a seed of 131.
/// </summary>
public enum Elf64Variant
{
    /// <summary>The default configuration (seed = 0).</summary>
    Default,

    /// <summary>Seed = 31.</summary>
    Seed31,

    /// <summary>Seed = 131.</summary>
    Seed131,
}
