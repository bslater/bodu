// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein1024AppendixBIVs.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds the Skein 1.3 Appendix B precomputed initial chaining values for the three <see cref="Skein1024" /> output
/// sizes. The constants are reproduced verbatim from the Skein 1.3 / NIST CD reference distribution so the test
/// suite can verify the production CFG-UBI derivation against the published values.
/// </summary>
internal static class Skein1024AppendixBIVs
{
    /// <summary>Skein-1024-384 initial chaining value (state size 1024 bits, output 384 bits).</summary>
    public static readonly IReadOnlyList<ulong> Skein1024_384 = new ulong[]
    {
        0x5102B6B8C1894A35UL,
        0xFEEBC9E3FE8AF11AUL,
        0x0C807F06E32BED71UL,
        0x60C13A52B41A91F6UL,
        0x9716D35DD4917C38UL,
        0xE780DF126FD31D3AUL,
        0x797846B6C898303AUL,
        0xB172C2A8B3572A3BUL,
        0xC9BC8203A6104A6CUL,
        0x65909338D75624F4UL,
        0x94BCC5684B3F81A0UL,
        0x3EBBF51E10ECFD46UL,
        0x2DF50F0BEEB08542UL,
        0x3B5A65300DBC6516UL,
        0x484B9CD2167BBCE1UL,
        0x2D136947D4CBAFEAUL,
    };

    /// <summary>Skein-1024-512 initial chaining value (state size 1024 bits, output 512 bits).</summary>
    public static readonly IReadOnlyList<ulong> Skein1024_512 = new ulong[]
    {
        0xCAEC0E5D7C1B1B18UL,
        0xA01B0E045F03E802UL,
        0x33840451ED912885UL,
        0x374AFB04EAEC2E1CUL,
        0xDF25A0E2813581F7UL,
        0xE40040938B12F9D2UL,
        0xA662D539C2ED39B6UL,
        0xFA8B85CF45D8C75AUL,
        0x8316ED8E29EDE796UL,
        0x053289C02E9F91B8UL,
        0xC3F8EF1D6D518B73UL,
        0xBDCEC3C4D5EF332EUL,
        0x549A7E5222974487UL,
        0x670708725B749816UL,
        0xB9CD28FBF0581BD1UL,
        0x0E2940B815804974UL,
    };

    /// <summary>Skein-1024-1024 initial chaining value (state size 1024 bits, output 1024 bits).</summary>
    public static readonly IReadOnlyList<ulong> Skein1024_1024 = new ulong[]
    {
        0xD593DA0741E72355UL,
        0x15B5E511AC73E00CUL,
        0x5180E5AEBAF2C4F0UL,
        0x03BD41D3FCBCAFAFUL,
        0x1CAEC6FD1983A898UL,
        0x6E510B8BCDD0589FUL,
        0x77E2BDFDC6394ADAUL,
        0xC11E1DB524DCB0A3UL,
        0xD6D14AF9C6329AB5UL,
        0x6A9B0BFC6EB67E0DUL,
        0x9243C60DCCFF1332UL,
        0x1A1F1DDE743F02D4UL,
        0x0996753C10ED0BB8UL,
        0x6572DD22F2B4969AUL,
        0x61FD3062D00A579AUL,
        0x1DE0536E8682E539UL,
    };
}
