// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKem.1024.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the ML-KEM-1024 parameter set of NIST FIPS 203 (module rank 4, NIST security category 5, comparable to
/// AES-256). This class cannot be inherited.
/// </summary>
/// <remarks>
/// Key and ciphertext sizes: encapsulation key 1568 bytes, decapsulation key 3168 bytes, ciphertext 1568 bytes, shared
/// secret 32 bytes. See <see cref="MLKem" /> for the shared algorithm surface and security notes.
/// </remarks>
public sealed class MLKem1024
    : MLKem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MLKem1024" /> class with no key material.
    /// </summary>
    public MLKem1024()
        : base(MLKemParameters.MLKem1024, 1024)
    { }

    /// <summary>
    /// Creates a new <see cref="MLKem1024" /> instance with no key material.
    /// </summary>
    /// <returns>A new <see cref="MLKem1024" /> instance.</returns>
    public static new MLKem1024 Create() =>
        new();
}
