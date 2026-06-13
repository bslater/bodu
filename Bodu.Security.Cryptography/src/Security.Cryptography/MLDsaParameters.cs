// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaParameters.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Captures the FIPS 204 parameter set of an ML-DSA instance: matrix dimensions, noise and rejection bounds, and
/// the derived encoding sizes.
/// </summary>
internal sealed class MLDsaParameters
{
    /// <summary>
    /// The parameters of ML-DSA-44 (k = 4, ℓ = 4, η = 2, τ = 39, γ₁ = 2¹⁷, γ₂ = (q−1)/88, ω = 80, λ = 128).
    /// </summary>
    internal static readonly MLDsaParameters MLDsa44 = new("ML-DSA-44", 4, 4, 2, 39, 1 << 17, (MLDsaEngine.Q - 1) / 88, 80, 128);

    /// <summary>
    /// The parameters of ML-DSA-65 (k = 6, ℓ = 5, η = 4, τ = 49, γ₁ = 2¹⁹, γ₂ = (q−1)/32, ω = 55, λ = 192).
    /// </summary>
    internal static readonly MLDsaParameters MLDsa65 = new("ML-DSA-65", 6, 5, 4, 49, 1 << 19, (MLDsaEngine.Q - 1) / 32, 55, 192);

    /// <summary>
    /// The parameters of ML-DSA-87 (k = 8, ℓ = 7, η = 2, τ = 60, γ₁ = 2¹⁹, γ₂ = (q−1)/32, ω = 75, λ = 256).
    /// </summary>
    internal static readonly MLDsaParameters MLDsa87 = new("ML-DSA-87", 8, 7, 2, 60, 1 << 19, (MLDsaEngine.Q - 1) / 32, 75, 256);

    /// <summary>
    /// Initializes a new instance of the <see cref="MLDsaParameters" /> class.
    /// </summary>
    /// <param name="name">The FIPS 204 parameter-set name.</param>
    /// <param name="k">The number of rows of the matrix Â.</param>
    /// <param name="l">The number of columns of the matrix Â.</param>
    /// <param name="eta">The secret-coefficient bound η.</param>
    /// <param name="tau">The number of ±1 coefficients τ in the challenge polynomial.</param>
    /// <param name="gamma1">The mask range γ₁.</param>
    /// <param name="gamma2">The low-order rounding range γ₂.</param>
    /// <param name="omega">The maximum total number of hint bits ω.</param>
    /// <param name="lambda">The collision-strength parameter λ in bits; the commitment hash is λ/4 bytes.</param>
    private MLDsaParameters(string name, int k, int l, int eta, int tau, int gamma1, int gamma2, int omega, int lambda)
    {
        Name = name;
        K = k;
        L = l;
        Eta = eta;
        Tau = tau;
        Gamma1 = gamma1;
        Gamma2 = gamma2;
        Omega = omega;
        Lambda = lambda;
        Beta = tau * eta;

        EtaBits = eta == 2 ? 3 : 4;
        Gamma1Bits = gamma1 == (1 << 17) ? 18 : 20;
        W1Bits = gamma2 == (MLDsaEngine.Q - 1) / 88 ? 6 : 4;

        PublicKeySize = 32 + (320 * k);
        PrivateKeySize = 128 + (32 * (((k + l) * EtaBits) + (13 * k)));
        SignatureSize = (lambda / 4) + (l * 32 * Gamma1Bits) + omega + k;
    }

    /// <summary>
    /// Gets the FIPS 204 parameter-set name, such as <c>"ML-DSA-65"</c>.
    /// </summary>
    /// <returns>The parameter-set name.</returns>
    internal string Name { get; }

    /// <summary>
    /// Gets the number of rows k of the matrix Â (and the length of t, s₂, and w).
    /// </summary>
    /// <returns>4, 6, or 8.</returns>
    internal int K { get; }

    /// <summary>
    /// Gets the number of columns ℓ of the matrix Â (and the length of s₁, y, and z).
    /// </summary>
    /// <returns>4, 5, or 7.</returns>
    internal int L { get; }

    /// <summary>
    /// Gets the secret-coefficient bound η.
    /// </summary>
    /// <returns>2 or 4.</returns>
    internal int Eta { get; }

    /// <summary>
    /// Gets the number of ±1 coefficients τ in the challenge polynomial.
    /// </summary>
    /// <returns>39, 49, or 60.</returns>
    internal int Tau { get; }

    /// <summary>
    /// Gets the mask range γ₁; the response z must satisfy ‖z‖∞ &lt; γ₁ − β.
    /// </summary>
    /// <returns>2¹⁷ or 2¹⁹.</returns>
    internal int Gamma1 { get; }

    /// <summary>
    /// Gets the low-order rounding range γ₂; decomposition uses α = 2γ₂.
    /// </summary>
    /// <returns>(q−1)/88 or (q−1)/32.</returns>
    internal int Gamma2 { get; }

    /// <summary>
    /// Gets the maximum total number of hint bits ω.
    /// </summary>
    /// <returns>80, 55, or 75.</returns>
    internal int Omega { get; }

    /// <summary>
    /// Gets the collision-strength parameter λ in bits; the commitment hash c̃ is λ/4 bytes long.
    /// </summary>
    /// <returns>128, 192, or 256.</returns>
    internal int Lambda { get; }

    /// <summary>
    /// Gets the rejection bound β = τ·η.
    /// </summary>
    /// <returns>78, 196, or 120.</returns>
    internal int Beta { get; }

    /// <summary>
    /// Gets the bit width used to pack a secret coefficient: bitlen(2η).
    /// </summary>
    /// <returns>3 when η = 2; 4 when η = 4.</returns>
    internal int EtaBits { get; }

    /// <summary>
    /// Gets the bit width used to pack a response coefficient: 1 + bitlen(γ₁ − 1).
    /// </summary>
    /// <returns>18 or 20.</returns>
    internal int Gamma1Bits { get; }

    /// <summary>
    /// Gets the bit width used to pack a w₁ coefficient: bitlen((q − 1)/(2γ₂) − 1).
    /// </summary>
    /// <returns>6 when γ₂ = (q−1)/88; 4 when γ₂ = (q−1)/32.</returns>
    internal int W1Bits { get; }

    /// <summary>
    /// Gets the encoded public key size in bytes: 32 + 320k.
    /// </summary>
    /// <returns>1312, 1952, or 2592.</returns>
    internal int PublicKeySize { get; }

    /// <summary>
    /// Gets the encoded private key size in bytes.
    /// </summary>
    /// <returns>2560, 4032, or 4896.</returns>
    internal int PrivateKeySize { get; }

    /// <summary>
    /// Gets the encoded signature size in bytes.
    /// </summary>
    /// <returns>2420, 3309, or 4627.</returns>
    internal int SignatureSize { get; }
}
