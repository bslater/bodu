// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkeinTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains shared fixtures and helpers for the <see cref="Skein256" />, <see cref="Skein512" />, and
/// <see cref="Skein1024" /> unit tests. The test methods themselves live in the adjacent partial-class files.
/// </summary>
[TestClass]
public partial class SkeinTests
{
    /// <summary>
    /// A deterministic byte pattern used to exercise streaming, block-alignment, and multi-block code paths.
    /// </summary>
    /// <param name="length">The number of bytes to produce.</param>
    /// <returns>A byte array of <paramref name="length" /> bytes whose content is reproducible across test runs.</returns>
    private static byte[] BuildDeterministicInput(int length)
    {
        byte[] buffer = new byte[length];
        for (int i = 0; i < length; i++)
            buffer[i] = (byte)((i * 31) + 7);

        return buffer;
    }
}
