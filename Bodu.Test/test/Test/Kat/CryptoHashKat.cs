// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHashKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Kat;

/// <summary>
/// Represents a known-answer test row for a cryptographic hash, keyed hash, or XOF algorithm. Carries the
/// raw input, the expected digest, an explicit output length (so XOFs that can be requested at multiple
/// lengths share the same record shape), and optional key / personalisation strings consumed by keyed
/// variants such as BLAKE2 and Skein.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Input">The raw byte payload supplied to the algorithm.</param>
/// <param name="ExpectedDigest">The expected digest bytes.</param>
/// <param name="OutputLengthBytes">The requested digest length in bytes; for fixed-output algorithms this matches the natural digest length.</param>
/// <param name="Key">An optional key for keyed-hash variants. <see langword="null" /> selects the unkeyed mode.</param>
/// <param name="Customization">An optional personalisation / customisation string used by Skein / BLAKE2 / SHAKE customisation modes.</param>
public sealed record CryptoHashKat(
    string Name,
    byte[] Input,
    byte[] ExpectedDigest,
    int OutputLengthBytes,
    byte[]? Key = null,
    byte[]? Customization = null) : IKat;
