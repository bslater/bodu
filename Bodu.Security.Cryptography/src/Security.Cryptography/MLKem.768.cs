// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKem.768.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the ML-KEM-768 parameter set of NIST FIPS 203 (module rank 3, NIST security category 3, comparable to
/// AES-192) — the parameter set most widely deployed for TLS hybrid key exchange. This class cannot be inherited.
/// </summary>
/// <remarks>
/// Key and ciphertext sizes: encapsulation key 1184 bytes, decapsulation key 2400 bytes, ciphertext 1088 bytes, shared
/// secret 32 bytes. See <see cref="MLKem" /> for the shared algorithm surface and security notes.
/// </remarks>
public sealed class MLKem768
    : MLKem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MLKem768" /> class with no key material.
    /// </summary>
    public MLKem768()
        : base(MLKemParameters.MLKem768, 768)
    { }

    /// <summary>
    /// Creates a new <see cref="MLKem768" /> instance with no key material.
    /// </summary>
    /// <returns>A new <see cref="MLKem768" /> instance.</returns>
    public static new MLKem768 Create() =>
        new();
}
