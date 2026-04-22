// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Elf64Tests.Elf64Variant.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Identifies the ELF64 seed configurations exercised by <see cref="Elf64Tests" />: the unseeded default,
    /// a seed of 31, and a seed of 131.
    /// </summary>
    public enum Elf64Variant
    {
        Default,
        Seed31,
        Seed131,
    }
}
