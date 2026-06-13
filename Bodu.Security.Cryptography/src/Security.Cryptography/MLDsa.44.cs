// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsa.44.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the ML-DSA-44 parameter set of NIST FIPS 204 (matrix 4×4, NIST security category 2). This class cannot be inherited.
/// </summary>
/// <remarks>
/// Encoding sizes: public key 1312 bytes, private key 2560 bytes, signature 2420 bytes. See <see cref="MLDsa" /> for the shared algorithm surface and security notes.
/// </remarks>
public sealed class MLDsa44
    : MLDsa
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MLDsa44" /> class with no key material.
    /// </summary>
    public MLDsa44()
        : base(MLDsaParameters.MLDsa44, 44)
    { }

    /// <summary>
    /// Creates a new <see cref="MLDsa44" /> instance with no key material.
    /// </summary>
    /// <returns>A new <see cref="MLDsa44" /> instance.</returns>
    public static new MLDsa44 Create() =>
        new();
}
