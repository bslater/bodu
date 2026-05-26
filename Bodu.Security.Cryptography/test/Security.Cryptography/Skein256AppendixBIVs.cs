// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein256AppendixBIVs.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds the Skein 1.3 Appendix B precomputed initial chaining values for the four <see cref="Skein256" /> output
/// sizes. The constants are reproduced verbatim from the Skein 1.3 / NIST CD reference distribution so the test
/// suite can verify the production CFG-UBI derivation against the published values.
/// </summary>
internal static class Skein256AppendixBIVs
{
    /// <summary>Skein-256-128 initial chaining value (state size 256 bits, output 128 bits).</summary>
    public static readonly IReadOnlyList<ulong> Skein256_128 =
    [
        0xE1111906964D7260UL,
        0x883DAAA77C8D811CUL,
        0x10080DF491960F7AUL,
        0xCCF7DDE5B45BC1C2UL,
    ];

    /// <summary>Skein-256-160 initial chaining value (state size 256 bits, output 160 bits).</summary>
    public static readonly IReadOnlyList<ulong> Skein256_160 =
    [
        0x1420231472825E98UL,
        0x2AC4E9A25A77E590UL,
        0xD47A58568838D63EUL,
        0x2DD2E4968586AB7DUL,
    ];

    /// <summary>Skein-256-224 initial chaining value (state size 256 bits, output 224 bits).</summary>
    public static readonly IReadOnlyList<ulong> Skein256_224 =
    [
        0xC6098A8C9AE5EA0BUL,
        0x876D568608C5191CUL,
        0x99CB88D7D7F53884UL,
        0x384BDDB1AEDDB5DEUL,
    ];

    /// <summary>Skein-256-256 initial chaining value (state size 256 bits, output 256 bits).</summary>
    public static readonly IReadOnlyList<ulong> Skein256_256 =
    [
        0xFC9DA860D048B449UL,
        0x2FCA66479FA7D833UL,
        0xB33BC3896656840FUL,
        0x6A54E920FDE8DA69UL,
    ];
}
