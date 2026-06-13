// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKem.512.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the ML-KEM-512 parameter set of NIST FIPS 203 (module rank 2, NIST security category 1, comparable to
/// AES-128). This class cannot be inherited.
/// </summary>
/// <remarks>
/// Key and ciphertext sizes: encapsulation key 800 bytes, decapsulation key 1632 bytes, ciphertext 768 bytes, shared
/// secret 32 bytes. See <see cref="MLKem" /> for the shared algorithm surface and security notes.
/// </remarks>
public sealed class MLKem512
    : MLKem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MLKem512" /> class with no key material.
    /// </summary>
    public MLKem512()
        : base(MLKemParameters.MLKem512, 512)
    { }

    /// <summary>
    /// Creates a new <see cref="MLKem512" /> instance with no key material.
    /// </summary>
    /// <returns>A new <see cref="MLKem512" /> instance.</returns>
    public static new MLKem512 Create() =>
        new();
}
