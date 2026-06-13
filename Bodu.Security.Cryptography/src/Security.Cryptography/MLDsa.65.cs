// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsa.65.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the ML-DSA-65 parameter set of NIST FIPS 204 (matrix 6×5, NIST security category 3 — the most widely recommended general-purpose set). This class cannot be inherited.
/// </summary>
/// <remarks>
/// Encoding sizes: public key 1952 bytes, private key 4032 bytes, signature 3309 bytes. See <see cref="MLDsa" /> for the shared algorithm surface and security notes.
/// </remarks>
public sealed class MLDsa65
    : MLDsa
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MLDsa65" /> class with no key material.
    /// </summary>
    public MLDsa65()
        : base(MLDsaParameters.MLDsa65, 65)
    { }

    /// <summary>
    /// Creates a new <see cref="MLDsa65" /> instance with no key material.
    /// </summary>
    /// <returns>A new <see cref="MLDsa65" /> instance.</returns>
    public static new MLDsa65 Create() =>
        new();
}
