// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemParameters.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Captures the FIPS 203 parameter set of an ML-KEM instance: the module rank and noise/compression parameters together
/// with the derived encoding sizes.
/// </summary>
internal sealed class MLKemParameters
{
    /// <summary>The parameters of ML-KEM-512 (k = 2, η₁ = 3, η₂ = 2, dᵤ = 10, dᵥ = 4).</summary>
    internal static readonly MLKemParameters MLKem512 = new("ML-KEM-512", 2, 3, 2, 10, 4);

    /// <summary>The parameters of ML-KEM-768 (k = 3, η₁ = 2, η₂ = 2, dᵤ = 10, dᵥ = 4).</summary>
    internal static readonly MLKemParameters MLKem768 = new("ML-KEM-768", 3, 2, 2, 10, 4);

    /// <summary>The parameters of ML-KEM-1024 (k = 4, η₁ = 2, η₂ = 2, dᵤ = 11, dᵥ = 5).</summary>
    internal static readonly MLKemParameters MLKem1024 = new("ML-KEM-1024", 4, 2, 2, 11, 5);

    /// <summary>
    /// Initializes a new instance of the <see cref="MLKemParameters" /> class.
    /// </summary>
    /// <param name="name">The FIPS 203 parameter-set name.</param>
    /// <param name="k">The module rank (matrix dimension).</param>
    /// <param name="eta1">The CBD noise parameter η₁ for key generation and the encryption secret.</param>
    /// <param name="eta2">The CBD noise parameter η₂ for the encryption error terms.</param>
    /// <param name="du">The compression bit width dᵤ of the ciphertext vector component.</param>
    /// <param name="dv">The compression bit width dᵥ of the ciphertext scalar component.</param>
    private MLKemParameters(string name, int k, int eta1, int eta2, int du, int dv)
    {
        Name = name;
        K = k;
        Eta1 = eta1;
        Eta2 = eta2;
        Du = du;
        Dv = dv;

        EncapsulationKeySize = (384 * k) + 32;
        DecapsulationKeySize = (768 * k) + 96;
        CiphertextSize = 32 * ((du * k) + dv);
    }

    /// <summary>
    /// Gets the FIPS 203 parameter-set name, such as <c>"ML-KEM-768"</c>.
    /// </summary>
    /// <value>The parameter-set name.</value>
    internal string Name { get; }

    /// <summary>
    /// Gets the module rank k — the dimension of the matrix Â and of the secret and noise vectors.
    /// </summary>
    /// <value>2, 3, or 4.</value>
    internal int K { get; }

    /// <summary>
    /// Gets the centered-binomial-distribution parameter η₁ used for the key-generation secret and error vectors and
    /// the encryption secret vector.
    /// </summary>
    /// <value>3 for ML-KEM-512; otherwise 2.</value>
    internal int Eta1 { get; }

    /// <summary>
    /// Gets the centered-binomial-distribution parameter η₂ used for the encryption error terms.
    /// </summary>
    /// <value>Always 2 for the standardized parameter sets.</value>
    internal int Eta2 { get; }

    /// <summary>
    /// Gets the compression bit width dᵤ applied to the ciphertext vector component u.
    /// </summary>
    /// <value>10 or 11.</value>
    internal int Du { get; }

    /// <summary>
    /// Gets the compression bit width dᵥ applied to the ciphertext scalar component v.
    /// </summary>
    /// <value>4 or 5.</value>
    internal int Dv { get; }

    /// <summary>
    /// Gets the encapsulation (public) key size in bytes: 384k + 32.
    /// </summary>
    /// <value>800, 1184, or 1568.</value>
    internal int EncapsulationKeySize { get; }

    /// <summary>
    /// Gets the decapsulation (private) key size in bytes: 768k + 96.
    /// </summary>
    /// <value>1632, 2400, or 3168.</value>
    internal int DecapsulationKeySize { get; }

    /// <summary>
    /// Gets the ciphertext size in bytes: 32(dᵤk + dᵥ).
    /// </summary>
    /// <value>768, 1088, or 1568.</value>
    internal int CiphertextSize { get; }
}
