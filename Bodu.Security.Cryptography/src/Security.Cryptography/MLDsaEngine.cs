// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngine.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the ML-DSA digital signature algorithm of NIST FIPS 204: module-lattice key generation, the
/// Fiat-Shamir-with-aborts signing loop, and verification.
/// </summary>
/// <remarks>
/// <para>
/// Polynomials are held as 256 <see cref="int" /> coefficients in [0, q) with q = 8380417; centered values are folded
/// modulo q and re-centered at the packing boundaries and norm checks. Coefficient reductions use the C# remainder
/// operator with the constant modulus, which the JIT lowers to multiply-and-shift sequences rather than data-dependent
/// division.
/// </para>
/// <para>
/// The number of rejection-loop restarts during signing is public by design (FIPS 204 §3.5); the per-iteration work is
/// fixed and the norm scans have no early exit.
/// </para>
/// </remarks>
internal static partial class MLDsaEngine
{
    /// <summary>
    /// The polynomial degree n = 256 shared by every parameter set.
    /// </summary>
    internal const int N = 256;

    /// <summary>
    /// The coefficient modulus q = 8380417.
    /// </summary>
    internal const int Q = 8380417;

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

        int[][] s1 = SamplePolyVector(parameters, rhoPrime, l, 0);
        int[][] s2 = SamplePolyVector(parameters, rhoPrime, k, l);

        // t = NTT⁻¹(Â ∘ NTT(s₁)) + s₂.
        int[][] s1Hat = ClonePolyVector(s1);
        foreach (int[] poly in s1Hat)
            Ntt(poly);

        int[][] t = MultiplyMatrixVector(parameters, rho, s1Hat);
        for (int i = 0; i < k; i++)
        {
            InvNtt(t[i]);
            AddInto(t[i], s2[i]);
        }

        EncodeKeys(parameters, rho, capK, t, s1, s2, publicKey, privateKey);

        CryptographyHelper.Clear(expanded);
        ClearPolyVector(s1);
        ClearPolyVector(s1Hat);
        ClearPolyVector(s2);
        ClearPolyVector(t);
    }

    /// <summary>
    /// Runs ML-DSA.Sign_internal (FIPS 204 Algorithm 7) over the message representative M′ = 0x00 ‖ len(ctx) ‖ ctx ‖ M.
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
    internal static void Sign(
        MLDsaParameters parameters,
        ReadOnlySpan<byte> privateKey,
        ReadOnlySpan<byte> context,
        ReadOnlySpan<byte> message,
        ReadOnlySpan<byte> rnd,
        Span<byte> signature)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(privateKey, parameters.PrivateKeySize);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(rnd, 32);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(signature, parameters.SignatureSize);

        int k = parameters.K;
        int l = parameters.L;

        DecodePrivateKey(
            parameters, privateKey,
            out var rho, out var capK, out var tr, out int[][]? s1Hat, out int[][]? s2Hat, out int[][]? t0Hat);

        // ŝ₁, ŝ₂, t̂₀ are kept in the NTT domain for the per-iteration products.
        foreach (int[] poly in s1Hat)
            Ntt(poly);
        foreach (int[] poly in s2Hat)
            Ntt(poly);
        foreach (int[] poly in t0Hat)
            Ntt(poly);

        int[][][] matrix = ExpandA(parameters, rho);

        // μ = H(tr ‖ M′, 64); ρ″ = H(K ‖ rnd ‖ μ, 64).
        Span<byte> mu = stackalloc byte[64];
        ComputeMu(tr, context, message, mu);

        Span<byte> rhoDoublePrime = stackalloc byte[64];
        var seedSponge = KeccakSponge.CreateShake256();
        seedSponge.Absorb(capK);
        seedSponge.Absorb(rnd);
        seedSponge.Absorb(mu);
        seedSponge.Squeeze(rhoDoublePrime);
        seedSponge.Clear();

        int[][] y = CreatePolyVector(l);
        int[][] w = CreatePolyVector(k);
        int[][] w1 = CreatePolyVector(k);
        int[][] z = CreatePolyVector(l);
        int[][] wMinusCs2 = CreatePolyVector(k);
        int[][] hints = CreatePolyVector(k);
        int[] c = new int[N];
        int[] product = new int[N];
        Span<byte> commitmentHash = signature[..(parameters.Lambda / 4)];
        byte[] w1Encoded = new byte[32 * parameters.W1Bits * k];

        for (int kappa = 0; ; kappa += l)
        {
            // y = ExpandMask(ρ″, κ); w = NTT⁻¹(Â ∘ NTT(y)); w₁ = HighBits(w).
            for (int r = 0; r < l; r++)
                ExpandMask(parameters, rhoDoublePrime, kappa + r, y[r]);

            int[][] yHat = ClonePolyVector(y);
            foreach (int[] poly in yHat)
                Ntt(poly);

            for (int i = 0; i < k; i++)
            {
                Array.Clear(w[i]);
                for (int s = 0; s < l; s++)
                {
                    MultiplyNtt(matrix[i][s], yHat[s], product);
                    AddInto(w[i], product);
                }

                InvNtt(w[i]);
                for (int j = 0; j < N; j++)
                    w1[i][j] = HighBits(parameters.Gamma2, w[i][j]);
            }

            ClearPolyVector(yHat);

            // c̃ = H(μ ‖ w1Encode(w₁), λ/4); c = SampleInBall(c̃); ĉ = NTT(c).
            W1Encode(parameters, w1, w1Encoded);
            KeccakSponge.Shake256(mu, w1Encoded, commitmentHash);
            SampleInBall(parameters, commitmentHash, c);
            Ntt(c);

            // z = y + NTT⁻¹(ĉ ∘ ŝ₁); reject when ‖z‖∞ ≥ γ₁ − β.
            bool rejected = false;
            for (int r = 0; r < l; r++)
            {
                MultiplyNtt(c, s1Hat[r], product);
                InvNtt(product);
                for (int j = 0; j < N; j++)
                    z[r][j] = (y[r][j] + product[j]) % Q;

                rejected |= InfinityNorm(z[r]) >= parameters.Gamma1 - parameters.Beta;
            }

            // r₀ = LowBits(w − NTT⁻¹(ĉ ∘ ŝ₂)); reject when ‖r₀‖∞ ≥ γ₂ − β.
            for (int i = 0; i < k && !rejected; i++)
            {
                MultiplyNtt(c, s2Hat[i], product);
                InvNtt(product);
                for (int j = 0; j < N; j++)
                    wMinusCs2[i][j] = (w[i][j] - product[j] + Q) % Q;

                int lowNorm = 0;
                for (int j = 0; j < N; j++)
                {
                    Decompose(parameters.Gamma2, wMinusCs2[i][j], out _, out int r0);
                    lowNorm = Math.Max(lowNorm, Math.Abs(r0));
                }

                rejected |= lowNorm >= parameters.Gamma2 - parameters.Beta;
            }

            if (rejected)
                continue;

            // h = MakeHint(−⟨ĉ ∘ t̂₀⟩, w − cs₂ + ct₀); reject when ‖ct₀‖∞ ≥ γ₂ or the hint weight exceeds ω.
            int hintWeight = 0;
            for (int i = 0; i < k && !rejected; i++)
            {
                MultiplyNtt(c, t0Hat[i], product);
                InvNtt(product);

                rejected |= InfinityNorm(product) >= parameters.Gamma2;

                for (int j = 0; j < N; j++)
                {
                    int negated = (Q - product[j]) % Q;
                    int basis = (wMinusCs2[i][j] + product[j]) % Q;
                    hints[i][j] = MakeHint(parameters.Gamma2, negated, basis);
                    hintWeight += hints[i][j];
                }
            }

            if (rejected || hintWeight > parameters.Omega)
                continue;

            EncodeSignature(parameters, z, hints, signature);
            break;
        }

        CryptographyHelper.Clear(rhoDoublePrime);
        CryptographyHelper.Clear(mu);
        CryptographyHelper.Clear(c);
        CryptographyHelper.Clear(product);
        ClearPolyVector(s1Hat);
        ClearPolyVector(s2Hat);
        ClearPolyVector(t0Hat);
        ClearPolyVector(y);
        ClearPolyVector(z);
        ClearPolyVector(w);
        ClearPolyVector(wMinusCs2);
    }

    /// <summary>
    /// Runs ML-DSA.Verify_internal (FIPS 204 Algorithm 8) over the message representative M′ = 0x00 ‖ len(ctx) ‖ ctx ‖
    /// M.
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
    internal static bool Verify(
        MLDsaParameters parameters,
        ReadOnlySpan<byte> publicKey,
        ReadOnlySpan<byte> context,
        ReadOnlySpan<byte> message,
        ReadOnlySpan<byte> signature)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(publicKey, parameters.PublicKeySize);

        if (signature.Length != parameters.SignatureSize)
            return false;

        int k = parameters.K;
        int l = parameters.L;
        int zBytes = 32 * parameters.Gamma1Bits;

        ReadOnlySpan<byte> rho = publicKey[..32];
        ReadOnlySpan<byte> commitmentHash = signature[..(parameters.Lambda / 4)];
        ReadOnlySpan<byte> zPacked = signature.Slice(parameters.Lambda / 4, l * zBytes);
        ReadOnlySpan<byte> hintPacked = signature[((parameters.Lambda / 4) + (l * zBytes))..];

        // Decode z and reject ‖z‖∞ ≥ γ₁ − β; reject non-canonical hint encodings.
        int[][] z = CreatePolyVector(l);
        for (int r = 0; r < l; r++)
        {
            BitUnpackSigned(parameters.Gamma1Bits, parameters.Gamma1, zPacked.Slice(r * zBytes, zBytes), z[r]);
            if (InfinityNorm(z[r]) >= parameters.Gamma1 - parameters.Beta)
                return false;
        }

        int[][] hints = CreatePolyVector(k);
        if (!TryHintBitUnpack(parameters, hintPacked, hints))
            return false;

        // μ = H(H(pk, 64) ‖ M′, 64).
        Span<byte> tr = stackalloc byte[64];
        var trSponge = KeccakSponge.CreateShake256();
        trSponge.Absorb(publicKey);
        trSponge.Squeeze(tr);
        trSponge.Clear();

        Span<byte> mu = stackalloc byte[64];
        ComputeMu(tr, context, message, mu);

        int[] c = new int[N];
        SampleInBall(parameters, commitmentHash, c);
        Ntt(c);

        foreach (int[] poly in z)
            Ntt(poly);

        int[][][] matrix = ExpandA(parameters, rho);

        // w′ ≈ NTT⁻¹(Â ∘ ẑ − ĉ ∘ NTT(t₁·2ᵈ)); w₁′ = UseHint(h, w′).
        int[] t1 = new int[N];
        int[] w = new int[N];
        int[] product = new int[N];
        int[][] w1 = CreatePolyVector(k);

        for (int i = 0; i < k; i++)
        {
            Array.Clear(w);
            for (int s = 0; s < l; s++)
            {
                MultiplyNtt(matrix[i][s], z[s], product);
                AddInto(w, product);
            }

            SimpleBitUnpack(10, publicKey.Slice(32 + (i * 320), 320), t1);
            for (int j = 0; j < N; j++)
                t1[j] = (int)(((long)t1[j] << D) % Q);

            Ntt(t1);
            MultiplyNtt(c, t1, product);
            SubtractFrom(w, product);
            InvNtt(w);

            for (int j = 0; j < N; j++)
                w1[i][j] = UseHint(parameters.Gamma2, hints[i][j], w[j]);
        }

        // Valid iff c̃ = H(μ ‖ w1Encode(w₁′), λ/4).
        byte[] w1Encoded = new byte[32 * parameters.W1Bits * k];
        W1Encode(parameters, w1, w1Encoded);

        Span<byte> expectedHash = stackalloc byte[64];
        KeccakSponge.Shake256(mu, w1Encoded, expectedHash[..(parameters.Lambda / 4)]);

        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            commitmentHash, expectedHash[..(parameters.Lambda / 4)]);
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

        int k = parameters.K;

        DecodePrivateKey(
            parameters, privateKey,
            out var rho, out _, out var tr, out int[][]? s1, out int[][]? s2, out int[][]? t0);

        // t = NTT⁻¹(Â ∘ NTT(s₁)) + s₂ = t₁·2ᵈ + t₀; rebuild t₁ and the pk encoding from it.
        int[][] s1Hat = ClonePolyVector(s1);
        foreach (int[] poly in s1Hat)
            Ntt(poly);

        int[][] t = MultiplyMatrixVector(parameters, rho, s1Hat);
        int[] t1 = new int[N];
        rho.CopyTo(publicKey[..32]);

        for (int i = 0; i < k; i++)
        {
            InvNtt(t[i]);
            AddInto(t[i], s2[i]);

            for (int j = 0; j < N; j++)
                Power2Round(t[i][j], out t1[j], out _);

            SimpleBitPack(10, t1, publicKey.Slice(32 + (i * 320), 320));
        }

        Span<byte> actualTr = stackalloc byte[64];
        var trSponge = KeccakSponge.CreateShake256();
        trSponge.Absorb(publicKey);
        trSponge.Squeeze(actualTr);
        trSponge.Clear();

        bool matches = System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(tr, actualTr);

        ClearPolyVector(s1);
        ClearPolyVector(s1Hat);
        ClearPolyVector(s2);
        ClearPolyVector(t0);
        ClearPolyVector(t);

        return matches;
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
    /// Expands the public matrix Â from ρ (FIPS 204 Algorithm 32 / ExpandA).
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="rho">The 32-byte matrix seed.</param>
    /// <returns>The k×ℓ matrix of NTT-domain polynomials.</returns>
    private static int[][][] ExpandA(MLDsaParameters parameters, ReadOnlySpan<byte> rho)
    {
        int[][][] matrix = new int[parameters.K][][];
        for (int r = 0; r < parameters.K; r++)
        {
            matrix[r] = new int[parameters.L][];
            for (int s = 0; s < parameters.L; s++)
            {
                matrix[r][s] = new int[N];
                RejNttPoly(rho, (byte)s, (byte)r, matrix[r][s]);
            }
        }

        return matrix;
    }

    /// <summary>
    /// Computes Â ∘ v̂ for an NTT-domain vector, returning the k accumulated NTT-domain products.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="rho">The 32-byte matrix seed.</param>
    /// <param name="vectorHat">The ℓ NTT-domain polynomials.</param>
    /// <returns>The k NTT-domain result polynomials.</returns>
    private static int[][] MultiplyMatrixVector(MLDsaParameters parameters, ReadOnlySpan<byte> rho, int[][] vectorHat)
    {
        int[][] result = CreatePolyVector(parameters.K);
        int[] row = new int[N];
        int[] product = new int[N];

        for (int i = 0; i < parameters.K; i++)
        {
            for (int s = 0; s < parameters.L; s++)
            {
                RejNttPoly(rho, (byte)s, (byte)i, row);
                MultiplyNtt(row, vectorHat[s], product);
                AddInto(result[i], product);
            }
        }

        return result;
    }

    /// <summary>
    /// Samples a vector of bounded secret polynomials via ExpandS (FIPS 204 Algorithm 33).
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="rhoPrime">The 64-byte secret expansion seed.</param>
    /// <param name="count">The number of polynomials to sample.</param>
    /// <param name="nonceBase">The starting nonce (0 for s₁, ℓ for s₂).</param>
    /// <returns>The sampled polynomials with centered coefficients folded into [0, q).</returns>
    private static int[][] SamplePolyVector(MLDsaParameters parameters, ReadOnlySpan<byte> rhoPrime, int count, int nonceBase)
    {
        int[][] vector = CreatePolyVector(count);
        for (int r = 0; r < count; r++)
            RejBoundedPoly(parameters.Eta, rhoPrime, nonceBase + r, vector[r]);

        return vector;
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
    /// <param name="publicKey">The span receiving the encoded public key.</param>
    /// <param name="privateKey">The span receiving the encoded private key.</param>
    private static void EncodeKeys(
        MLDsaParameters parameters,
        ReadOnlySpan<byte> rho,
        ReadOnlySpan<byte> capK,
        int[][] t,
        int[][] s1,
        int[][] s2,
        Span<byte> publicKey,
        Span<byte> privateKey)
    {
        int k = parameters.K;
        int l = parameters.L;
        int etaBytes = 32 * parameters.EtaBits;

        int[] t1 = new int[N];
        int[] t0 = new int[N];

        rho.CopyTo(publicKey[..32]);
        rho.CopyTo(privateKey[..32]);
        capK.CopyTo(privateKey.Slice(32, 32));

        Span<byte> t0Section = privateKey[(128 + ((k + l) * etaBytes))..];
        for (int i = 0; i < k; i++)
        {
            for (int j = 0; j < N; j++)
            {
                Power2Round(t[i][j], out t1[j], out int low);
                t0[j] = (low + Q) % Q;
            }

            SimpleBitPack(10, t1, publicKey.Slice(32 + (i * 320), 320));
            BitPackSigned(13, 1 << (D - 1), t0, t0Section.Slice(i * 32 * 13, 32 * 13));
        }

        for (int r = 0; r < l; r++)
            BitPackSigned(parameters.EtaBits, parameters.Eta, s1[r], privateKey.Slice(128 + (r * etaBytes), etaBytes));

        for (int r = 0; r < k; r++)
            BitPackSigned(parameters.EtaBits, parameters.Eta, s2[r], privateKey.Slice(128 + ((l + r) * etaBytes), etaBytes));

        // tr = H(pk, 64) is stored inside the private key for the signing digest.
        Span<byte> tr = privateKey.Slice(64, 64);
        var sponge = KeccakSponge.CreateShake256();
        sponge.Absorb(publicKey);
        sponge.Squeeze(tr);
        sponge.Clear();

        CryptographyHelper.Clear(t0);
    }

    /// <summary>
    /// Decodes the FIPS 204 private key layout into its seed segments and standard-domain polynomial vectors.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="privateKey">The encoded private key.</param>
    /// <param name="rho">Receives the 32-byte matrix seed slice.</param>
    /// <param name="capK">Receives the 32-byte signing seed slice.</param>
    /// <param name="tr">Receives the 64-byte public-key hash slice.</param>
    /// <param name="s1">Receives the ℓ decoded s₁ polynomials.</param>
    /// <param name="s2">Receives the k decoded s₂ polynomials.</param>
    /// <param name="t0">Receives the k decoded t₀ polynomials.</param>
    private static void DecodePrivateKey(
        MLDsaParameters parameters,
        ReadOnlySpan<byte> privateKey,
        out ReadOnlySpan<byte> rho,
        out ReadOnlySpan<byte> capK,
        out ReadOnlySpan<byte> tr,
        out int[][] s1,
        out int[][] s2,
        out int[][] t0)
    {
        int k = parameters.K;
        int l = parameters.L;
        int etaBytes = 32 * parameters.EtaBits;

        rho = privateKey[..32];
        capK = privateKey.Slice(32, 32);
        tr = privateKey.Slice(64, 64);

        s1 = CreatePolyVector(l);
        for (int r = 0; r < l; r++)
            BitUnpackSigned(parameters.EtaBits, parameters.Eta, privateKey.Slice(128 + (r * etaBytes), etaBytes), s1[r]);

        s2 = CreatePolyVector(k);
        for (int r = 0; r < k; r++)
            BitUnpackSigned(parameters.EtaBits, parameters.Eta, privateKey.Slice(128 + ((l + r) * etaBytes), etaBytes), s2[r]);

        t0 = CreatePolyVector(k);
        ReadOnlySpan<byte> t0Section = privateKey[(128 + ((k + l) * etaBytes))..];
        for (int r = 0; r < k; r++)
            BitUnpackSigned(13, 1 << (D - 1), t0Section.Slice(r * 32 * 13, 32 * 13), t0[r]);
    }

    /// <summary>
    /// Encodes the response vector and hints into the signature layout following the commitment hash already written at
    /// the front of the buffer.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="z">The ℓ response polynomials.</param>
    /// <param name="hints">The k hint polynomials.</param>
    /// <param name="signature">The full signature buffer; the commitment hash occupies its first λ/4 bytes.</param>
    private static void EncodeSignature(MLDsaParameters parameters, int[][] z, int[][] hints, Span<byte> signature)
    {
        int zBytes = 32 * parameters.Gamma1Bits;
        Span<byte> zSection = signature.Slice(parameters.Lambda / 4, parameters.L * zBytes);

        for (int r = 0; r < parameters.L; r++)
            BitPackSigned(parameters.Gamma1Bits, parameters.Gamma1, z[r], zSection.Slice(r * zBytes, zBytes));

        HintBitPack(parameters, hints, signature[((parameters.Lambda / 4) + (parameters.L * zBytes))..]);
    }

    /// <summary>
    /// Allocates a vector of zeroed 256-coefficient polynomials.
    /// </summary>
    /// <param name="count">The number of polynomials.</param>
    /// <returns>The allocated vector.</returns>
    private static int[][] CreatePolyVector(int count)
    {
        int[][] vector = new int[count][];
        for (int i = 0; i < count; i++)
            vector[i] = new int[N];

        return vector;
    }

    /// <summary>
    /// Deep-copies a polynomial vector.
    /// </summary>
    /// <param name="source">The vector to copy.</param>
    /// <returns>The copy.</returns>
    private static int[][] ClonePolyVector(int[][] source)
    {
        int[][] copy = new int[source.Length][];
        for (int i = 0; i < source.Length; i++)
            copy[i] = (int[])source[i].Clone();

        return copy;
    }

    /// <summary>
    /// Zeroes every polynomial of a vector holding secret-derived values.
    /// </summary>
    /// <param name="vector">The vector to clear.</param>
    private static void ClearPolyVector(int[][] vector)
    {
        foreach (int[] poly in vector)
            CryptographyHelper.Clear(poly);
    }
}
