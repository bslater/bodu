// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngine.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the ML-DSA digital signature algorithm of NIST FIPS 204: module-lattice key generation, the
/// Fiat-Shamir-with-aborts signing loop, and verification.
/// </summary>
/// <remarks>
/// <para>
/// Polynomials are held as 256 <see cref="int" /> coefficients and vectors of them flat, polynomial i at offset 256i,
/// in workspaces rented from <see cref="ArrayPool{T}.Shared" /> and cleared before they are returned. Between
/// operations coefficients lie in [0, q) with q = 8380417; centered values are folded modulo q and re-centered at the
/// packing boundaries and norm checks.
/// </para>
/// <para>
/// Within the transforms and products, coefficients are reduced by Montgomery reduction and a Barrett-style reduction
/// (see the Reduction partial): fixed sequences of multiplications, shifts and masks, never a division. Of the two
/// factors in each coefficient-wise product, the one fixed for the whole operation — the matrix Â, ŝ₁, ŝ₂, t̂₀, or
/// t̂₁·2ᵈ — is held in Montgomery form, so each product needs a single reduction and comes out exact.
/// </para>
/// <para>
/// The number of rejection-loop restarts during signing is public by design (FIPS 204 §3.5); the per-iteration work is
/// fixed and the norm scans have no early exit.
/// </para>
/// <para>
/// Signing and verification each come in two forms. One takes the encoded key and derives from it, on every call, the
/// matrix Â and the key's vectors in the form the arithmetic uses. The other takes those values as
/// <see cref="MLDsaKeyMaterial" /> keeps them, computed once when the key is set.
/// </para>
/// </remarks>
internal static partial class MLDsaEngine
{
    /// <summary>The polynomial degree n = 256 shared by every parameter set.</summary>
    internal const int N = 256;

    /// <summary>The coefficient modulus q = 8380417.</summary>
    internal const int Q = 8380417;

    /// <summary>2^13 · 2^64 mod q: <see cref="MontgomeryReduce" /> of a coefficient of t₁ times this is t₁ · 2ᵈ in Montgomery form.</summary>
    private const long PowerOfTwoDMontgomery = 6346488;

    /// <summary>
    /// Runs ML-DSA.KeyGen_internal (FIPS 204 Algorithm 6) from the 32-byte seed ξ, producing the encoded key pair.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="xi">The 32-byte key-generation seed ξ.</param>
    /// <param name="publicKey">The span receiving the encoded public key ρ ‖ t₁.</param>
    /// <param name="privateKey">The span receiving the encoded private key ρ ‖ K ‖ tr ‖ s₁ ‖ s₂ ‖ t₀.</param>
    /// <exception cref="ArgumentException">A span does not have its exact required length.</exception>
    internal static void KeyGen(
        MLDsaParameters parameters,
        ReadOnlySpan<byte> xi,
        Span<byte> publicKey,
        Span<byte> privateKey)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(xi, 32);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(publicKey, parameters.PublicKeySize);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(privateKey, parameters.PrivateKeySize);

        KeyGenCore(parameters, xi, publicKey, privateKey, [], [], [], [], [], keepValues: false);
    }

    /// <summary>
    /// Runs ML-DSA.KeyGen_internal (FIPS 204 Algorithm 6) from the 32-byte seed ξ, producing the encoded key pair and
    /// the values a key keeps for signing and verification.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="xi">The 32-byte key-generation seed ξ.</param>
    /// <param name="publicKey">The span receiving the encoded public key ρ ‖ t₁.</param>
    /// <param name="privateKey">The span receiving the encoded private key ρ ‖ K ‖ tr ‖ s₁ ‖ s₂ ‖ t₀.</param>
    /// <param name="matrix">The span receiving Â, as <see cref="ExpandMatrix" /> produces it.</param>
    /// <param name="highOrderVector">
    /// The span receiving NTT(t₁·2ᵈ), as <see cref="ExpandPublicKey" /> produces it.
    /// </param>
    /// <param name="secretVector1">The span receiving ŝ₁, as <see cref="ExpandPrivateKey" /> produces it.</param>
    /// <param name="secretVector2">The span receiving ŝ₂, as <see cref="ExpandPrivateKey" /> produces it.</param>
    /// <param name="lowOrderVector">The span receiving t̂₀, as <see cref="ExpandPrivateKey" /> produces it.</param>
    /// <exception cref="ArgumentException">A span does not have its exact required length.</exception>
    internal static void KeyGen(
        MLDsaParameters parameters,
        ReadOnlySpan<byte> xi,
        Span<byte> publicKey,
        Span<byte> privateKey,
        Span<int> matrix,
        Span<int> highOrderVector,
        Span<int> secretVector1,
        Span<int> secretVector2,
        Span<int> lowOrderVector)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(xi, 32);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(publicKey, parameters.PublicKeySize);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(privateKey, parameters.PrivateKeySize);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(matrix, parameters.K * parameters.L * N);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(highOrderVector, parameters.K * N);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(secretVector1, parameters.L * N);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(secretVector2, parameters.K * N);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(lowOrderVector, parameters.K * N);

        KeyGenCore(
            parameters, xi, publicKey, privateKey, matrix, highOrderVector, secretVector1, secretVector2, lowOrderVector, keepValues: true);
    }

    /// <summary>
    /// Runs ML-DSA.Sign_internal (FIPS 204 Algorithm 7) over the message representative M′ = 0x00 ‖ len(ctx) ‖ ctx ‖ M,
    /// deriving from the encoded private key the values signing uses.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="privateKey">The encoded private key.</param>
    /// <param name="context">The signature context string (at most 255 bytes; validated by the caller).</param>
    /// <param name="message">The message bytes.</param>
    /// <param name="rnd">
    /// The 32-byte signer randomness: fresh random for hedged signing, all-zero for deterministic.
    /// </param>
    /// <param name="signature">The span receiving the encoded signature c̃ ‖ z ‖ h.</param>
    /// <exception cref="ArgumentException">A fixed-size span does not have its exact required length.</exception>
    /// <remarks>
    /// The matrix Â and the vectors ŝ₁, ŝ₂ and t̂₀ are derived on every call. A key used for more than one signature
    /// keeps them instead and signs through the overload that takes them.
    /// </remarks>
    internal static void Sign(
        MLDsaParameters parameters,
        ReadOnlySpan<byte> privateKey,
        ReadOnlySpan<byte> context,
        ReadOnlySpan<byte> message,
        ReadOnlySpan<byte> rnd,
        Span<byte> signature)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(privateKey, parameters.PrivateKeySize);

        int k = parameters.K;
        int l = parameters.L;

        // Â, then ŝ₁, ŝ₂ and t̂₀.
        int length = ((k * l) + l + (2 * k)) * N;
        int[] rented = ArrayPool<int>.Shared.Rent(length);

        try
        {
            Span<int> workspace = rented.AsSpan(0, length);
            Span<int> matrix = TakePolynomials(ref workspace, k * l);
            Span<int> secretVector1 = TakePolynomials(ref workspace, l);
            Span<int> secretVector2 = TakePolynomials(ref workspace, k);
            Span<int> lowOrderVector = TakePolynomials(ref workspace, k);

            ExpandMatrix(parameters, privateKey[..32], matrix);
            ExpandPrivateKey(parameters, privateKey, secretVector1, secretVector2, lowOrderVector);

            Sign(parameters, privateKey, matrix, secretVector1, secretVector2, lowOrderVector, context, message, rnd, signature);
        }
        finally
        {
            ReturnWorkspace(rented, length);
        }
    }

    /// <summary>
    /// Runs ML-DSA.Sign_internal (FIPS 204 Algorithm 7) over the message representative M′ = 0x00 ‖ len(ctx) ‖ ctx ‖ M,
    /// with the values derived from the private key supplied as a key keeps them.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="privateKey">The encoded private key, from which the seed K and the hash tr are read.</param>
    /// <param name="matrix">The matrix Â, as <see cref="ExpandMatrix" /> produces it.</param>
    /// <param name="secretVector1">The vector ŝ₁, as <see cref="ExpandPrivateKey" /> produces it.</param>
    /// <param name="secretVector2">The vector ŝ₂, as <see cref="ExpandPrivateKey" /> produces it.</param>
    /// <param name="lowOrderVector">The vector t̂₀, as <see cref="ExpandPrivateKey" /> produces it.</param>
    /// <param name="context">The signature context string (at most 255 bytes; validated by the caller).</param>
    /// <param name="message">The message bytes.</param>
    /// <param name="rnd">
    /// The 32-byte signer randomness: fresh random for hedged signing, all-zero for deterministic.
    /// </param>
    /// <param name="signature">The span receiving the encoded signature c̃ ‖ z ‖ h.</param>
    /// <exception cref="ArgumentException">A fixed-size span does not have its exact required length.</exception>
    internal static void Sign(
        MLDsaParameters parameters,
        ReadOnlySpan<byte> privateKey,
        ReadOnlySpan<int> matrix,
        ReadOnlySpan<int> secretVector1,
        ReadOnlySpan<int> secretVector2,
        ReadOnlySpan<int> lowOrderVector,
        ReadOnlySpan<byte> context,
        ReadOnlySpan<byte> message,
        ReadOnlySpan<byte> rnd,
        Span<byte> signature)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(privateKey, parameters.PrivateKeySize);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(matrix, parameters.K * parameters.L * N);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(secretVector1, parameters.L * N);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(secretVector2, parameters.K * N);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(lowOrderVector, parameters.K * N);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(rnd, 32);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(signature, parameters.SignatureSize);

        int k = parameters.K;
        int l = parameters.L;

        // y, ŷ, z, w, w₁, w − cs₂, the hints, c and one product.
        int length = ((3 * l) + (4 * k) + 2) * N;
        int[] rented = ArrayPool<int>.Shared.Rent(length);

        Span<byte> mu = stackalloc byte[64];
        Span<byte> rhoDoublePrime = stackalloc byte[64];
        Span<byte> w1Encoded = stackalloc byte[32 * parameters.W1Bits * k];

        try
        {
            Span<int> workspace = rented.AsSpan(0, length);
            Span<int> y = TakePolynomials(ref workspace, l);
            Span<int> yHat = TakePolynomials(ref workspace, l);
            Span<int> z = TakePolynomials(ref workspace, l);
            Span<int> w = TakePolynomials(ref workspace, k);
            Span<int> w1 = TakePolynomials(ref workspace, k);
            Span<int> wMinusCs2 = TakePolynomials(ref workspace, k);
            Span<int> hints = TakePolynomials(ref workspace, k);
            Span<int> c = TakePolynomials(ref workspace, 1);
            Span<int> product = TakePolynomials(ref workspace, 1);

            // μ = H(tr ‖ M′, 64); ρ″ = H(K ‖ rnd ‖ μ, 64).
            ComputeMu(privateKey.Slice(64, 64), context, message, mu);

            var seedSponge = KeccakSponge.CreateShake256();
            seedSponge.Absorb(privateKey.Slice(32, 32));
            seedSponge.Absorb(rnd);
            seedSponge.Absorb(mu);
            seedSponge.Squeeze(rhoDoublePrime);
            seedSponge.Clear();

            int commitmentHashSize = parameters.Lambda / 4;
            Span<byte> commitmentHash = signature[..commitmentHashSize];

            for (int kappa = 0; ; kappa += l)
            {
                // y = ExpandMask(ρ″, κ); w = NTT⁻¹(Â ∘ NTT(y)); w₁ = HighBits(w).
                ExpandMaskVector(parameters, rhoDoublePrime, kappa, y);

                y.CopyTo(yHat);
                for (int r = 0; r < l; r++)
                    Ntt(yHat.Slice(r * N, N));

                MultiplyMatrixVector(parameters, matrix, yHat, w);

                for (int i = 0; i < k; i++)
                {
                    Span<int> wi = w.Slice(i * N, N);
                    InvNtt(wi);
                    HighBits(parameters.Gamma2, wi, w1.Slice(i * N, N));
                }

                // c̃ = H(μ ‖ w1Encode(w₁), λ/4); c = SampleInBall(c̃); ĉ = NTT(c).
                W1Encode(parameters, w1, w1Encoded);
                KeccakSponge.Shake256(mu, w1Encoded, commitmentHash);
                SampleInBall(parameters, commitmentHash, c);
                Ntt(c);

                // Each restart attempt computes z, r₀, and the hints in full and makes a single accept-or-restart
                // decision at the end. The number of restarts is public by design (FIPS 204 §3.5), but the work within one
                // attempt is kept independent of which check ultimately fails so a failed attempt is not distinguishable,
                // by the work it performs, from any other.
                bool rejected = false;

                // z = y + NTT⁻¹(ĉ ∘ ŝ₁); reject when ‖z‖∞ ≥ γ₁ − β.
                for (int r = 0; r < l; r++)
                {
                    MultiplyNtt(secretVector1.Slice(r * N, N), c, product);
                    InvNtt(product);

                    Span<int> zr = z.Slice(r * N, N);
                    AddModQ(y.Slice(r * N, N), product, zr);
                    rejected |= InfinityNorm(zr) >= parameters.Gamma1 - parameters.Beta;
                }

                // r₀ = LowBits(w − NTT⁻¹(ĉ ∘ ŝ₂)); reject when ‖r₀‖∞ ≥ γ₂ − β.
                for (int i = 0; i < k; i++)
                {
                    MultiplyNtt(secretVector2.Slice(i * N, N), c, product);
                    InvNtt(product);

                    Span<int> ri = wMinusCs2.Slice(i * N, N);
                    SubtractModQ(w.Slice(i * N, N), product, ri);
                    rejected |= LowBitsNorm(parameters.Gamma2, ri) >= parameters.Gamma2 - parameters.Beta;
                }

                // h = MakeHint(−⟨ĉ ∘ t̂₀⟩, w − cs₂ + ct₀); reject when ‖ct₀‖∞ ≥ γ₂ or the hint weight exceeds ω.
                int hintWeight = 0;
                for (int i = 0; i < k; i++)
                {
                    MultiplyNtt(lowOrderVector.Slice(i * N, N), c, product);
                    InvNtt(product);

                    rejected |= InfinityNorm(product) >= parameters.Gamma2;
                    hintWeight += MakeHints(parameters.Gamma2, product, wMinusCs2.Slice(i * N, N), hints.Slice(i * N, N));
                }

                if (rejected || hintWeight > parameters.Omega)
                    continue;

                EncodeSignature(parameters, z, hints, signature);
                break;
            }
        }
        finally
        {
            CryptographyHelper.Clear(rhoDoublePrime);
            CryptographyHelper.Clear(mu);
            ReturnWorkspace(rented, length);
        }
    }

    /// <summary>
    /// Runs ML-DSA.Verify_internal (FIPS 204 Algorithm 8) over the message representative M′ = 0x00 ‖ len(ctx) ‖ ctx ‖
    /// M, deriving from the encoded public key the values verification uses.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="publicKey">The encoded public key ρ ‖ t₁.</param>
    /// <param name="context">The signature context string (at most 255 bytes; validated by the caller).</param>
    /// <param name="message">The message bytes.</param>
    /// <param name="signature">The candidate encoded signature.</param>
    /// <returns><see langword="true" /> when the signature is valid; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="publicKey" /> does not have its exact required length.
    /// </exception>
    /// <remarks>
    /// The hash tr, the matrix Â and the vector NTT(t₁·2ᵈ) are derived on every call. A key used for more than one
    /// verification keeps them instead and verifies through the overload that takes them.
    /// </remarks>
    internal static bool Verify(
        MLDsaParameters parameters,
        ReadOnlySpan<byte> publicKey,
        ReadOnlySpan<byte> context,
        ReadOnlySpan<byte> message,
        ReadOnlySpan<byte> signature)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(publicKey, parameters.PublicKeySize);

        int k = parameters.K;
        int l = parameters.L;

        // Â, then NTT(t₁·2ᵈ).
        int length = ((k * l) + k) * N;
        int[] rented = ArrayPool<int>.Shared.Rent(length);

        Span<byte> publicKeyHash = stackalloc byte[64];

        try
        {
            Span<int> workspace = rented.AsSpan(0, length);
            Span<int> matrix = TakePolynomials(ref workspace, k * l);
            Span<int> highOrderVector = TakePolynomials(ref workspace, k);

            KeccakSponge.Shake256(publicKey, publicKeyHash);
            ExpandMatrix(parameters, publicKey[..32], matrix);
            ExpandPublicKey(parameters, publicKey, highOrderVector);

            return Verify(parameters, publicKeyHash, matrix, highOrderVector, context, message, signature);
        }
        finally
        {
            ReturnWorkspace(rented, length);
        }
    }

    /// <summary>
    /// Runs ML-DSA.Verify_internal (FIPS 204 Algorithm 8) over the message representative M′ = 0x00 ‖ len(ctx) ‖ ctx ‖
    /// M, with the values derived from the public key supplied as a key keeps them.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="publicKeyHash">The 64-byte hash tr = H(pk, 64) of the encoded public key.</param>
    /// <param name="matrix">The matrix Â, as <see cref="ExpandMatrix" /> produces it.</param>
    /// <param name="highOrderVector">The vector NTT(t₁·2ᵈ), as <see cref="ExpandPublicKey" /> produces it.</param>
    /// <param name="context">The signature context string (at most 255 bytes; validated by the caller).</param>
    /// <param name="message">The message bytes.</param>
    /// <param name="signature">The candidate encoded signature.</param>
    /// <returns><see langword="true" /> when the signature is valid; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentException">
    /// A span derived from the key does not have its exact required length.
    /// </exception>
    internal static bool Verify(
        MLDsaParameters parameters,
        ReadOnlySpan<byte> publicKeyHash,
        ReadOnlySpan<int> matrix,
        ReadOnlySpan<int> highOrderVector,
        ReadOnlySpan<byte> context,
        ReadOnlySpan<byte> message,
        ReadOnlySpan<byte> signature)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(publicKeyHash, 64);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(matrix, parameters.K * parameters.L * N);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(highOrderVector, parameters.K * N);

        if (signature.Length != parameters.SignatureSize)
            return false;

        int k = parameters.K;
        int l = parameters.L;
        int zBytes = 32 * parameters.Gamma1Bits;
        int commitmentHashSize = parameters.Lambda / 4;

        ReadOnlySpan<byte> commitmentHash = signature[..commitmentHashSize];
        ReadOnlySpan<byte> zPacked = signature.Slice(commitmentHashSize, l * zBytes);
        ReadOnlySpan<byte> hintPacked = signature.Slice(commitmentHashSize + (l * zBytes));

        // z, the hints, w, w₁, c and one product.
        int length = (l + (3 * k) + 2) * N;
        int[] rented = ArrayPool<int>.Shared.Rent(length);

        Span<byte> mu = stackalloc byte[64];
        Span<byte> expectedHash = stackalloc byte[64];
        Span<byte> w1Encoded = stackalloc byte[32 * parameters.W1Bits * k];

        try
        {
            Span<int> workspace = rented.AsSpan(0, length);
            Span<int> z = TakePolynomials(ref workspace, l);
            Span<int> hints = TakePolynomials(ref workspace, k);
            Span<int> w = TakePolynomials(ref workspace, k);
            Span<int> w1 = TakePolynomials(ref workspace, k);
            Span<int> c = TakePolynomials(ref workspace, 1);
            Span<int> product = TakePolynomials(ref workspace, 1);

            // Decode z and reject ‖z‖∞ ≥ γ₁ − β; reject non-canonical hint encodings.
            for (int r = 0; r < l; r++)
            {
                Span<int> zr = z.Slice(r * N, N);
                BitUnpackSigned(parameters.Gamma1Bits, parameters.Gamma1, zPacked.Slice(r * zBytes, zBytes), zr);
                if (InfinityNorm(zr) >= parameters.Gamma1 - parameters.Beta)
                    return false;
            }

            if (!TryHintBitUnpack(parameters, hintPacked, hints))
                return false;

            // μ = H(tr ‖ M′, 64).
            ComputeMu(publicKeyHash, context, message, mu);

            SampleInBall(parameters, commitmentHash, c);
            Ntt(c);

            for (int r = 0; r < l; r++)
                Ntt(z.Slice(r * N, N));

            // w′ ≈ NTT⁻¹(Â ∘ ẑ − ĉ ∘ NTT(t₁·2ᵈ)); w₁′ = UseHint(h, w′).
            MultiplyMatrixVector(parameters, matrix, z, w);

            for (int i = 0; i < k; i++)
            {
                Span<int> wi = w.Slice(i * N, N);
                MultiplyNtt(highOrderVector.Slice(i * N, N), c, product);
                for (int j = 0; j < N; j++)
                    wi[j] = Reduce32(wi[j] - product[j]);

                InvNtt(wi);

                for (int j = 0; j < N; j++)
                    w1[(i * N) + j] = UseHint(parameters.Gamma2, hints[(i * N) + j], wi[j]);
            }

            // Valid iff c̃ = H(μ ‖ w1Encode(w₁′), λ/4).
            W1Encode(parameters, w1, w1Encoded);
            KeccakSponge.Shake256(mu, w1Encoded, expectedHash[..commitmentHashSize]);

            return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                commitmentHash, expectedHash[..commitmentHashSize]);
        }
        finally
        {
            ReturnWorkspace(rented, length);
        }
    }

    /// <summary>
    /// Decodes the secret vectors of an encoded private key into the form signing uses: ŝ₁, ŝ₂ and t̂₀, each in the NTT
    /// domain and in Montgomery form.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="privateKey">The encoded private key.</param>
    /// <param name="secretVector1">The span receiving the ℓ polynomials of ŝ₁.</param>
    /// <param name="secretVector2">The span receiving the k polynomials of ŝ₂.</param>
    /// <param name="lowOrderVector">The span receiving the k polynomials of t̂₀.</param>
    /// <exception cref="ArgumentException">A span does not have its exact required length.</exception>
    /// <remarks>
    /// The key is not checked here: key generation produces well-formed keys, and import checks them.
    /// </remarks>
    internal static void ExpandPrivateKey(
        MLDsaParameters parameters,
        ReadOnlySpan<byte> privateKey,
        Span<int> secretVector1,
        Span<int> secretVector2,
        Span<int> lowOrderVector)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(privateKey, parameters.PrivateKeySize);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(secretVector1, parameters.L * N);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(secretVector2, parameters.K * N);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(lowOrderVector, parameters.K * N);

        _ = DecodePrivateKey(parameters, privateKey, out _, out _, out _, secretVector1, secretVector2, lowOrderVector);

        ToNttMontgomery(secretVector1);
        ToNttMontgomery(secretVector2);
        ToNttMontgomery(lowOrderVector);
    }

    /// <summary>
    /// Decodes t₁ from an encoded public key into the form verification uses: NTT(t₁·2ᵈ), in Montgomery form.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="publicKey">The encoded public key ρ ‖ t₁.</param>
    /// <param name="highOrderVector">The span receiving the k polynomials.</param>
    /// <exception cref="ArgumentException">A span does not have its exact required length.</exception>
    internal static void ExpandPublicKey(MLDsaParameters parameters, ReadOnlySpan<byte> publicKey, Span<int> highOrderVector)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(publicKey, parameters.PublicKeySize);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(highOrderVector, parameters.K * N);

        for (int i = 0; i < parameters.K; i++)
        {
            Span<int> t1 = highOrderVector.Slice(i * N, N);
            SimpleBitUnpack(10, publicKey.Slice(32 + (i * 320), 320), t1);

            // t₁·2ᵈ is formed directly in Montgomery form, so its transform multiplies ĉ exactly.
            for (int j = 0; j < N; j++)
                t1[j] = MontgomeryReduce(t1[j] * PowerOfTwoDMontgomery);

            Ntt(t1);
        }
    }

    /// <summary>
    /// Recomputes the encoded public key from a private key and reports whether the embedded tr hash matches, providing
    /// an import-time consistency check and the cached public key.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="privateKey">The encoded private key.</param>
    /// <param name="publicKey">The span receiving the recomputed encoded public key.</param>
    /// <returns><see langword="true" /> when tr = H(pk, 64) holds; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentException">A span does not have its exact required length.</exception>
    internal static bool TryDerivePublicKey(
        MLDsaParameters parameters,
        ReadOnlySpan<byte> privateKey,
        Span<byte> publicKey)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(privateKey, parameters.PrivateKeySize);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(publicKey, parameters.PublicKeySize);

        return TryDerivePublicKeyCore(parameters, privateKey, publicKey, [], [], [], [], [], keepValues: false);
    }

    /// <summary>
    /// Recomputes the encoded public key from a private key and reports whether the embedded tr hash matches, producing
    /// as well the values a key keeps for signing and verification.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="privateKey">The encoded private key.</param>
    /// <param name="publicKey">The span receiving the recomputed encoded public key.</param>
    /// <param name="matrix">The span receiving Â, as <see cref="ExpandMatrix" /> produces it.</param>
    /// <param name="highOrderVector">
    /// The span receiving NTT(t₁·2ᵈ), as <see cref="ExpandPublicKey" /> produces it.
    /// </param>
    /// <param name="secretVector1">The span receiving ŝ₁, as <see cref="ExpandPrivateKey" /> produces it.</param>
    /// <param name="secretVector2">The span receiving ŝ₂, as <see cref="ExpandPrivateKey" /> produces it.</param>
    /// <param name="lowOrderVector">The span receiving t̂₀, as <see cref="ExpandPrivateKey" /> produces it.</param>
    /// <returns><see langword="true" /> when tr = H(pk, 64) holds; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentException">A span does not have its exact required length.</exception>
    /// <remarks>
    /// The value spans are filled whether or not the key is consistent. When it is not, the caller discards them, and
    /// clears the secret ones.
    /// </remarks>
    internal static bool TryDerivePublicKey(
        MLDsaParameters parameters,
        ReadOnlySpan<byte> privateKey,
        Span<byte> publicKey,
        Span<int> matrix,
        Span<int> highOrderVector,
        Span<int> secretVector1,
        Span<int> secretVector2,
        Span<int> lowOrderVector)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(privateKey, parameters.PrivateKeySize);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(publicKey, parameters.PublicKeySize);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(matrix, parameters.K * parameters.L * N);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(highOrderVector, parameters.K * N);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(secretVector1, parameters.L * N);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(secretVector2, parameters.K * N);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(lowOrderVector, parameters.K * N);

        return TryDerivePublicKeyCore(
            parameters, privateKey, publicKey, matrix, highOrderVector, secretVector1, secretVector2, lowOrderVector, keepValues: true);
    }

    /// <summary>
    /// Expands the public matrix Â from ρ (FIPS 204 Algorithm 32 / ExpandA), in Montgomery form.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="rho">The 32-byte matrix seed.</param>
    /// <param name="matrix">
    /// The span receiving the k·ℓ NTT-domain polynomials in Montgomery form, entry (r, s) at offset (r·ℓ + s)·256.
    /// </param>
    /// <exception cref="ArgumentException">A span does not have its exact required length.</exception>
    internal static void ExpandMatrix(MLDsaParameters parameters, ReadOnlySpan<byte> rho, Span<int> matrix)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(rho, 32);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(matrix, parameters.K * parameters.L * N);

        SampleMatrix(parameters, rho, matrix);
        for (int offset = 0; offset < matrix.Length; offset += N)
            ToMontgomery(matrix.Slice(offset, N));
    }

    /// <summary>
    /// Runs ML-DSA.KeyGen_internal (FIPS 204 Algorithm 6) for both
    /// <see cref="KeyGen(MLDsaParameters, ReadOnlySpan{byte}, Span{byte}, Span{byte})" /> overloads, whose arguments
    /// are already validated.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="xi">The 32-byte key-generation seed ξ.</param>
    /// <param name="publicKey">The span receiving the encoded public key.</param>
    /// <param name="privateKey">The span receiving the encoded private key.</param>
    /// <param name="matrix">The span receiving Â, or an empty span when the values are not kept.</param>
    /// <param name="highOrderVector">The span receiving NTT(t₁·2ᵈ), or an empty span.</param>
    /// <param name="secretVector1">The span receiving ŝ₁, or an empty span.</param>
    /// <param name="secretVector2">The span receiving ŝ₂, or an empty span.</param>
    /// <param name="lowOrderVector">The span receiving t̂₀, or an empty span.</param>
    /// <param name="keepValues">
    /// <see langword="true" /> to fill the five value spans; <see langword="false" /> when they are empty.
    /// </param>
    private static void KeyGenCore(
        MLDsaParameters parameters,
        ReadOnlySpan<byte> xi,
        Span<byte> publicKey,
        Span<byte> privateKey,
        Span<int> matrix,
        Span<int> highOrderVector,
        Span<int> secretVector1,
        Span<int> secretVector2,
        Span<int> lowOrderVector,
        bool keepValues)
    {
        int k = parameters.K;
        int l = parameters.L;

        // (ρ, ρ′, K) = H(ξ ‖ k ‖ ℓ, 128).
        Span<byte> dims = stackalloc byte[2];
        dims[0] = (byte)k;
        dims[1] = (byte)l;

        Span<byte> expanded = stackalloc byte[128];
        var sponge = KeccakSponge.CreateShake256();
        sponge.Absorb(xi);
        sponge.Absorb(dims);
        sponge.Squeeze(expanded);
        sponge.Clear();

        ReadOnlySpan<byte> rho = expanded[..32];
        ReadOnlySpan<byte> rhoPrime = expanded.Slice(32, 64);
        ReadOnlySpan<byte> capK = expanded[96..];

        // s₁ and s₂, ŝ₁, t and t₀, then Â when the caller does not keep it.
        int length = ((2 * l) + (3 * k) + (keepValues ? 0 : k * l)) * N;
        int[] rented = ArrayPool<int>.Shared.Rent(length);

        try
        {
            Span<int> workspace = rented.AsSpan(0, length);

            // s₁ and s₂ are adjacent, so ExpandS samples them as one vector, nonces 0 to ℓ + k − 1.
            Span<int> secrets = TakePolynomials(ref workspace, l + k);
            Span<int> s1 = secrets[..(l * N)];
            Span<int> s2 = secrets[(l * N)..];
            Span<int> s1Hat = TakePolynomials(ref workspace, l);
            Span<int> t = TakePolynomials(ref workspace, k);
            Span<int> t0 = TakePolynomials(ref workspace, k);
            Span<int> expandedMatrix = keepValues ? matrix : TakePolynomials(ref workspace, k * l);

            SampleSecretVector(parameters, rhoPrime, 0, secrets);
            ExpandMatrix(parameters, rho, expandedMatrix);

            // t = NTT⁻¹(Â ∘ NTT(s₁)) + s₂. Â is held in Montgomery form, so ŝ₁ stays plain for the products.
            s1.CopyTo(s1Hat);
            for (int r = 0; r < l; r++)
                Ntt(s1Hat.Slice(r * N, N));

            MultiplyMatrixVector(parameters, expandedMatrix, s1Hat, t);
            if (keepValues)
                t.CopyTo(highOrderVector);

            for (int i = 0; i < k; i++)
            {
                Span<int> ti = t.Slice(i * N, N);
                InvNtt(ti);
                AddModQ(ti, s2.Slice(i * N, N), ti);
            }

            EncodeKeys(parameters, rho, capK, t, s1, s2, t0, publicKey, privateKey);

            if (keepValues)
                KeepKeyValues(s1Hat, s2, t0, highOrderVector, secretVector1, secretVector2, lowOrderVector);
        }
        finally
        {
            CryptographyHelper.Clear(expanded);
            ReturnWorkspace(rented, length);
        }
    }

    /// <summary>
    /// Recomputes the encoded public key from a private key for both
    /// <see cref="TryDerivePublicKey(MLDsaParameters, ReadOnlySpan{byte}, Span{byte})" /> overloads, whose arguments
    /// are already validated.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="privateKey">The encoded private key.</param>
    /// <param name="publicKey">The span receiving the recomputed encoded public key.</param>
    /// <param name="matrix">The span receiving Â, or an empty span when the values are not kept.</param>
    /// <param name="highOrderVector">The span receiving NTT(t₁·2ᵈ), or an empty span.</param>
    /// <param name="secretVector1">The span receiving ŝ₁, or an empty span.</param>
    /// <param name="secretVector2">The span receiving ŝ₂, or an empty span.</param>
    /// <param name="lowOrderVector">The span receiving t̂₀, or an empty span.</param>
    /// <param name="keepValues">
    /// <see langword="true" /> to fill the five value spans; <see langword="false" /> when they are empty.
    /// </param>
    /// <returns><see langword="true" /> when tr = H(pk, 64) holds; otherwise, <see langword="false" />.</returns>
    private static bool TryDerivePublicKeyCore(
        MLDsaParameters parameters,
        ReadOnlySpan<byte> privateKey,
        Span<byte> publicKey,
        Span<int> matrix,
        Span<int> highOrderVector,
        Span<int> secretVector1,
        Span<int> secretVector2,
        Span<int> lowOrderVector,
        bool keepValues)
    {
        int k = parameters.K;
        int l = parameters.L;

        // s₁, ŝ₁, s₂, t₀, t and t₁, then Â when the caller does not keep it.
        int length = ((2 * l) + (3 * k) + 1 + (keepValues ? 0 : k * l)) * N;
        int[] rented = ArrayPool<int>.Shared.Rent(length);

        Span<byte> actualTr = stackalloc byte[64];

        try
        {
            Span<int> workspace = rented.AsSpan(0, length);
            Span<int> s1 = TakePolynomials(ref workspace, l);
            Span<int> s1Hat = TakePolynomials(ref workspace, l);
            Span<int> s2 = TakePolynomials(ref workspace, k);
            Span<int> t0 = TakePolynomials(ref workspace, k);
            Span<int> t = TakePolynomials(ref workspace, k);
            Span<int> t1 = TakePolynomials(ref workspace, 1);
            Span<int> expandedMatrix = keepValues ? matrix : TakePolynomials(ref workspace, k * l);

            // s₁/s₂ are rejected here when packed outside [−η, η]; t₀ spans the full d-bit range, so it is validated
            // below by comparing the decoded low bits against those recomputed from s₁/s₂.
            bool valid = DecodePrivateKey(
                parameters, privateKey, out ReadOnlySpan<byte> rho, out _, out ReadOnlySpan<byte> tr, s1, s2, t0);

            // t = NTT⁻¹(Â ∘ NTT(s₁)) + s₂ = t₁·2ᵈ + t₀; rebuild t₁ and the pk encoding from it.
            ExpandMatrix(parameters, rho, expandedMatrix);
            s1.CopyTo(s1Hat);
            for (int r = 0; r < l; r++)
                Ntt(s1Hat.Slice(r * N, N));

            MultiplyMatrixVector(parameters, expandedMatrix, s1Hat, t);
            if (keepValues)
                t.CopyTo(highOrderVector);

            rho.CopyTo(publicKey[..32]);

            bool t0Matches = true;
            for (int i = 0; i < k; i++)
            {
                Span<int> ti = t.Slice(i * N, N);
                InvNtt(ti);
                AddModQ(ti, s2.Slice(i * N, N), ti);

                for (int j = 0; j < N; j++)
                {
                    Power2Round(ti[j], out t1[j], out int recomputedT0);

                    // The encoded t₀ stores the centered low bits folded into [0, q); unfold before comparing, through a
                    // mask rather than a branch. The scan has no early exit so a corrupted coefficient is not revealed
                    // by timing.
                    int decodedT0 = t0[(i * N) + j];
                    decodedT0 -= Q & ((((Q - 1) / 2) - decodedT0) >> 31);

                    t0Matches &= decodedT0 == recomputedT0;
                }

                SimpleBitPack(10, t1, publicKey.Slice(32 + (i * 320), 320));
            }

            KeccakSponge.Shake256(publicKey, actualTr);

            bool matches = System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(tr, actualTr);

            if (keepValues)
                KeepKeyValues(s1Hat, s2, t0, highOrderVector, secretVector1, secretVector2, lowOrderVector);

            return valid & t0Matches & matches;
        }
        finally
        {
            ReturnWorkspace(rented, length);
        }
    }

    /// <summary>
    /// Forms the values a key keeps for signing and verification from what key generation and import compute along the
    /// way, sparing the transforms <see cref="ExpandPrivateKey" /> and <see cref="ExpandPublicKey" /> would repeat.
    /// </summary>
    /// <param name="s1Hat">The ℓ polynomials of NTT(s₁), plain.</param>
    /// <param name="s2">The k polynomials of s₂, coefficients in [0, q).</param>
    /// <param name="t0">The k polynomials of t₀, coefficients in [0, q) as the private key encodes them.</param>
    /// <param name="highOrderVector">
    /// Holds Â ∘ NTT(s₁) on entry; receives NTT(t₁·2ᵈ), as <see cref="ExpandPublicKey" /> produces it.
    /// </param>
    /// <param name="secretVector1">The span receiving ŝ₁, as <see cref="ExpandPrivateKey" /> produces it.</param>
    /// <param name="secretVector2">The span receiving ŝ₂, as <see cref="ExpandPrivateKey" /> produces it.</param>
    /// <param name="lowOrderVector">The span receiving t̂₀, as <see cref="ExpandPrivateKey" /> produces it.</param>
    /// <remarks>
    /// The transform is linear, so NTT(t₁·2ᵈ) = NTT(t) − t̂₀ = Â ∘ ŝ₁ + ŝ₂ − t̂₀, and the high-order vector comes from
    /// values already at hand rather than from k more transforms. Each value is brought to the representative the
    /// Expand methods produce, so what a key keeps does not depend on how the key was set.
    /// </remarks>
    private static void KeepKeyValues(
        ReadOnlySpan<int> s1Hat,
        ReadOnlySpan<int> s2,
        ReadOnlySpan<int> t0,
        Span<int> highOrderVector,
        Span<int> secretVector1,
        Span<int> secretVector2,
        Span<int> lowOrderVector)
    {
        s1Hat.CopyTo(secretVector1);
        for (int offset = 0; offset < secretVector1.Length; offset += N)
            ToMontgomery(secretVector1.Slice(offset, N));

        s2.CopyTo(secretVector2);
        t0.CopyTo(lowOrderVector);
        for (int offset = 0; offset < secretVector2.Length; offset += N)
        {
            Ntt(secretVector2.Slice(offset, N));
            Ntt(lowOrderVector.Slice(offset, N));
        }

        // Â ∘ ŝ₁ arrives reduced by Reduce32, so the sum below stays well within Freeze's range.
        for (int j = 0; j < highOrderVector.Length; j++)
        {
            int plain = Freeze(highOrderVector[j] + secretVector2[j] - lowOrderVector[j]);
            highOrderVector[j] = Canonicalize(ToMontgomery(plain));
        }

        for (int offset = 0; offset < secretVector2.Length; offset += N)
        {
            ToMontgomery(secretVector2.Slice(offset, N));
            ToMontgomery(lowOrderVector.Slice(offset, N));
        }
    }

    /// <summary>
    /// Computes the message digest μ = H(tr ‖ 0x00 ‖ len(ctx) ‖ ctx ‖ M, 64) without materializing the concatenated
    /// message representative.
    /// </summary>
    /// <param name="tr">The 64-byte public-key hash from the private key or recomputed from the public key.</param>
    /// <param name="context">The signature context string.</param>
    /// <param name="message">The message bytes.</param>
    /// <param name="mu">The 64-byte span receiving μ.</param>
    private static void ComputeMu(ReadOnlySpan<byte> tr, ReadOnlySpan<byte> context, ReadOnlySpan<byte> message, Span<byte> mu)
    {
        Span<byte> framing = stackalloc byte[2];
        framing[0] = 0;
        framing[1] = (byte)context.Length;

        var sponge = KeccakSponge.CreateShake256();
        sponge.Absorb(tr);
        sponge.Absorb(framing);
        sponge.Absorb(context);
        sponge.Absorb(message);
        sponge.Squeeze(mu);
        sponge.Clear();
    }

    /// <summary>
    /// Computes Â ∘ v̂ for the matrix in Montgomery form and a plain NTT-domain vector.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="matrix">The matrix Â, as <see cref="ExpandMatrix" /> produces it.</param>
    /// <param name="vector">The ℓ NTT-domain polynomials.</param>
    /// <param name="result">
    /// The span receiving the k NTT-domain products, plain, each coefficient reduced with <see cref="Reduce32(int)" />
    /// so that <see cref="InvNtt(Span{int})" /> takes it as it is.
    /// </param>
    private static void MultiplyMatrixVector(
        MLDsaParameters parameters,
        ReadOnlySpan<int> matrix,
        ReadOnlySpan<int> vector,
        Span<int> result)
    {
        int l = parameters.L;

        for (int i = 0; i < parameters.K; i++)
        {
            Span<int> ri = result.Slice(i * N, N);
            ri.Clear();

            for (int s = 0; s < l; s++)
                MultiplyAccumulateNtt(matrix.Slice(((i * l) + s) * N, N), vector.Slice(s * N, N), ri);

            Reduce32(ri);
        }
    }

    /// <summary>
    /// Transforms each polynomial of a vector to the NTT domain and converts it to Montgomery form, in place.
    /// </summary>
    /// <param name="vector">The polynomials, coefficients in [0, q).</param>
    private static void ToNttMontgomery(Span<int> vector)
    {
        for (int offset = 0; offset < vector.Length; offset += N)
        {
            Span<int> poly = vector.Slice(offset, N);
            Ntt(poly);
            ToMontgomery(poly);
        }
    }

    /// <summary>
    /// Encodes the freshly generated key material into the FIPS 204 public and private key layouts.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="rho">The 32-byte matrix seed.</param>
    /// <param name="capK">The 32-byte signing seed K.</param>
    /// <param name="t">The k polynomials t = As₁ + s₂, split by Power2Round during encoding.</param>
    /// <param name="s1">The ℓ secret polynomials s₁.</param>
    /// <param name="s2">The k secret polynomials s₂.</param>
    /// <param name="t0">
    /// The span receiving the k polynomials t₀ that Power2Round splits from t, coefficients folded into [0, q).
    /// </param>
    /// <param name="publicKey">The span receiving the encoded public key.</param>
    /// <param name="privateKey">The span receiving the encoded private key.</param>
    private static void EncodeKeys(
        MLDsaParameters parameters,
        ReadOnlySpan<byte> rho,
        ReadOnlySpan<byte> capK,
        ReadOnlySpan<int> t,
        ReadOnlySpan<int> s1,
        ReadOnlySpan<int> s2,
        Span<int> t0,
        Span<byte> publicKey,
        Span<byte> privateKey)
    {
        int k = parameters.K;
        int l = parameters.L;
        int etaBytes = 32 * parameters.EtaBits;

        Span<int> t1 = stackalloc int[N];

        rho.CopyTo(publicKey[..32]);
        rho.CopyTo(privateKey[..32]);
        capK.CopyTo(privateKey.Slice(32, 32));

        Span<byte> t0Section = privateKey[(128 + ((k + l) * etaBytes))..];
        for (int i = 0; i < k; i++)
        {
            Span<int> t0i = t0.Slice(i * N, N);
            for (int j = 0; j < N; j++)
            {
                Power2Round(t[(i * N) + j], out t1[j], out int low);
                t0i[j] = Canonicalize(low);
            }

            SimpleBitPack(10, t1, publicKey.Slice(32 + (i * 320), 320));
            BitPackSigned(13, 1 << (D - 1), t0i, t0Section.Slice(i * 32 * 13, 32 * 13));
        }

        for (int r = 0; r < l; r++)
            BitPackSigned(parameters.EtaBits, parameters.Eta, s1.Slice(r * N, N), privateKey.Slice(128 + (r * etaBytes), etaBytes));

        for (int r = 0; r < k; r++)
            BitPackSigned(parameters.EtaBits, parameters.Eta, s2.Slice(r * N, N), privateKey.Slice(128 + ((l + r) * etaBytes), etaBytes));

        // tr = H(pk, 64) is stored inside the private key for the signing digest.
        Span<byte> tr = privateKey.Slice(64, 64);
        var sponge = KeccakSponge.CreateShake256();
        sponge.Absorb(publicKey);
        sponge.Squeeze(tr);
        sponge.Clear();
    }

    /// <summary>
    /// Decodes the FIPS 204 private key layout into its seed segments and standard-domain polynomial vectors.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="privateKey">The encoded private key.</param>
    /// <param name="rho">Receives the 32-byte matrix seed slice.</param>
    /// <param name="capK">Receives the 32-byte signing seed slice.</param>
    /// <param name="tr">Receives the 64-byte public-key hash slice.</param>
    /// <param name="s1">The span receiving the ℓ decoded s₁ polynomials.</param>
    /// <param name="s2">The span receiving the k decoded s₂ polynomials.</param>
    /// <param name="t0">The span receiving the k decoded t₀ polynomials.</param>
    /// <returns>
    /// <see langword="true" /> when every s₁/s₂ coefficient is packed within its canonical [−η, η] range; otherwise,
    /// <see langword="false" />. The t₀ packing spans the full d-bit range, so its consistency is validated separately
    /// by recomputation in <see cref="TryDerivePublicKeyCore" />.
    /// </returns>
    private static bool DecodePrivateKey(
        MLDsaParameters parameters,
        ReadOnlySpan<byte> privateKey,
        out ReadOnlySpan<byte> rho,
        out ReadOnlySpan<byte> capK,
        out ReadOnlySpan<byte> tr,
        Span<int> s1,
        Span<int> s2,
        Span<int> t0)
    {
        int k = parameters.K;
        int l = parameters.L;
        int etaBytes = 32 * parameters.EtaBits;
        int maxEncoded = 2 * parameters.Eta;

        rho = privateKey[..32];
        capK = privateKey.Slice(32, 32);
        tr = privateKey.Slice(64, 64);

        bool valid = true;

        for (int r = 0; r < l; r++)
            valid &= TryBitUnpackSigned(parameters.EtaBits, parameters.Eta, maxEncoded, privateKey.Slice(128 + (r * etaBytes), etaBytes), s1.Slice(r * N, N));

        for (int r = 0; r < k; r++)
            valid &= TryBitUnpackSigned(parameters.EtaBits, parameters.Eta, maxEncoded, privateKey.Slice(128 + ((l + r) * etaBytes), etaBytes), s2.Slice(r * N, N));

        ReadOnlySpan<byte> t0Section = privateKey[(128 + ((k + l) * etaBytes))..];
        for (int r = 0; r < k; r++)
            BitUnpackSigned(13, 1 << (D - 1), t0Section.Slice(r * 32 * 13, 32 * 13), t0.Slice(r * N, N));

        return valid;
    }

    /// <summary>
    /// Encodes the response vector and hints into the signature layout following the commitment hash already written at
    /// the front of the buffer.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="z">The ℓ response polynomials.</param>
    /// <param name="hints">The k hint polynomials.</param>
    /// <param name="signature">The full signature buffer; the commitment hash occupies its first λ/4 bytes.</param>
    private static void EncodeSignature(MLDsaParameters parameters, ReadOnlySpan<int> z, ReadOnlySpan<int> hints, Span<byte> signature)
    {
        int zBytes = 32 * parameters.Gamma1Bits;
        Span<byte> zSection = signature.Slice(parameters.Lambda / 4, parameters.L * zBytes);

        for (int r = 0; r < parameters.L; r++)
            BitPackSigned(parameters.Gamma1Bits, parameters.Gamma1, z.Slice(r * N, N), zSection.Slice(r * zBytes, zBytes));

        HintBitPack(parameters, hints, signature[((parameters.Lambda / 4) + (parameters.L * zBytes))..]);
    }

    /// <summary>
    /// Takes the next <paramref name="count" /> polynomials from the front of a workspace, advancing it past them.
    /// </summary>
    /// <param name="workspace">The remaining workspace, advanced past the polynomials taken.</param>
    /// <param name="count">The number of 256-coefficient polynomials to take.</param>
    /// <returns>The polynomials taken.</returns>
    private static Span<int> TakePolynomials(ref Span<int> workspace, int count)
    {
        int length = count * N;
        Span<int> taken = workspace[..length];
        workspace = workspace[length..];
        return taken;
    }

    /// <summary>
    /// Clears the used part of a rented workspace, which holds secret-derived values, and returns it to the pool.
    /// </summary>
    /// <param name="rented">The array rented from <see cref="ArrayPool{T}.Shared" />.</param>
    /// <param name="length">The number of leading elements that were used.</param>
    private static void ReturnWorkspace(int[] rented, int length)
    {
        CryptographyHelper.Clear(rented.AsSpan(0, length));
        ArrayPool<int>.Shared.Return(rented);
    }
}
