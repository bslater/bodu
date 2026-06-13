// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsa.87.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the ML-DSA-87 parameter set of NIST FIPS 204 (matrix 8×7, NIST security category 5). This class cannot be
/// inherited.
/// </summary>
/// <remarks>
/// Encoding sizes: public key 2592 bytes, private key 4896 bytes, signature 4627 bytes. See <see cref="MLDsa" /> for
/// the shared algorithm surface and security notes.
/// </remarks>
public sealed class MLDsa87
    : MLDsa
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MLDsa87" /> class with no key material.
    /// </summary>
    public MLDsa87()
        : base(MLDsaParameters.MLDsa87, 87)
    { }

    /// <summary>
    /// Creates a new <see cref="MLDsa87" /> instance with no key material.
    /// </summary>
    /// <returns>A new <see cref="MLDsa87" /> instance.</returns>
    public static new MLDsa87 Create() =>
        new();
}
