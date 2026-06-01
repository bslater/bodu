// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein512AppendixBIVs.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds the Skein 1.3 Appendix B precomputed initial chaining values for the six <see cref="Skein512" /> output sizes.
/// The constants are reproduced verbatim from the Skein 1.3 / NIST CD reference distribution so the test suite can
/// verify the production CFG-UBI derivation against the published values.
/// </summary>
internal static class Skein512AppendixBIVs
{
    /// <summary>
    /// Skein-512-128 initial chaining value (state size 512 bits, output 128 bits).
    /// </summary>
    public static readonly IReadOnlyList<ulong> Skein512_128 =
    [
        0xA8BC7BF36FBF9F52UL,
        0x1E9872CEBD1AF0AAUL,
        0x309B1790B32190D3UL,
        0xBCFBB8543F94805CUL,
        0x0DA61BCD6E31B11BUL,
        0x1A18EBEAD46A32E3UL,
        0xA2CC5B18CE84AA82UL,
        0x6982AB289D46982DUL,
    ];

    /// <summary>
    /// Skein-512-160 initial chaining value (state size 512 bits, output 160 bits).
    /// </summary>
    public static readonly IReadOnlyList<ulong> Skein512_160 =
    [
        0x28B81A2AE013BD91UL,
        0xC2F11668B5BDF78FUL,
        0x1760D8F3F6A56F12UL,
        0x4FB747588239904FUL,
        0x21EDE07F7EAF5056UL,
        0xD908922E63ED70B8UL,
        0xB8EC76FFECCB52FAUL,
        0x01A47BB8A3F27A6EUL,
    ];

    /// <summary>
    /// Skein-512-224 initial chaining value (state size 512 bits, output 224 bits).
    /// </summary>
    public static readonly IReadOnlyList<ulong> Skein512_224 =
    [
        0xCCD0616248677224UL,
        0xCBA65CF3A92339EFUL,
        0x8CCD69D652FF4B64UL,
        0x398AED7B3AB890B4UL,
        0x0F59D1B1457D2BD0UL,
        0x6776FE6575D4EB3DUL,
        0x99FBC70E997413E9UL,
        0x9E2CFCCFE1C41EF7UL,
    ];

    /// <summary>
    /// Skein-512-256 initial chaining value (state size 512 bits, output 256 bits).
    /// </summary>
    public static readonly IReadOnlyList<ulong> Skein512_256 =
    [
        0xCCD044A12FDB3E13UL,
        0xE83590301A79A9EBUL,
        0x55AEA0614F816E6FUL,
        0x2A2767A4AE9B94DBUL,
        0xEC06025E74DD7683UL,
        0xE7A436CDC4746251UL,
        0xC36FBAF9393AD185UL,
        0x3EEDBA1833EDFC13UL,
    ];

    /// <summary>
    /// Skein-512-384 initial chaining value (state size 512 bits, output 384 bits).
    /// </summary>
    public static readonly IReadOnlyList<ulong> Skein512_384 =
    [
        0xA3F6C6BF3A75EF5FUL,
        0xB0FEF9CCFD84FAA4UL,
        0x9D77DD663D770CFEUL,
        0xD798CBF3B468FDDAUL,
        0x1BC4A6668A0E4465UL,
        0x7ED7D434E5807407UL,
        0x548FC1ACD4EC44D6UL,
        0x266E17546AA18FF8UL,
    ];

    /// <summary>
    /// Skein-512-512 initial chaining value (state size 512 bits, output 512 bits).
    /// </summary>
    public static readonly IReadOnlyList<ulong> Skein512_512 =
    [
        0x4903ADFF749C51CEUL,
        0x0D95DE399746DF03UL,
        0x8FD1934127C79BCEUL,
        0x9A255629FF352CB1UL,
        0x5DB62599DF6CA7B0UL,
        0xEABE394CA9D5C3F4UL,
        0x991112C71A75B523UL,
        0xAE18A40B660FCC33UL,
    ];
}
