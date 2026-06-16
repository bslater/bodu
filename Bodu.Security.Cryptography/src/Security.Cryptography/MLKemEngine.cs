// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemEngine.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the ML-KEM key-encapsulation mechanism of NIST FIPS 203: module-lattice key generation, the K-PKE
/// encryption core, and the Fujisaki-Okamoto encapsulate/decapsulate transform with implicit rejection.
/// </summary>
/// <remarks>
/// <para>
/// Polynomials are held as 256 <see cref="int" /> coefficients in [0, q) with q = 3329. Coefficient reductions use the
/// C# remainder operator with the constant modulus, which the JIT lowers to multiply-and-shift sequences rather than
/// data-dependent division. The decapsulation re-encryption comparison and the secret selection between the real and
/// implicit-rejection keys are byte-wise constant-time.
/// </para>
/// </remarks>
internal static partial class MLKemEngine
{
    /// <summary>
    /// The polynomial degree n = 256 shared by every parameter set.
    /// </summary>
    internal const int N = 256;

    /// <summary>
    /// The coefficient modulus q = 3329.
    /// </summary>
    internal const int Q = 3329;

    /// <summary>
    /// The size, in bytes, of the shared secret produced by encapsulation and decapsulation.
    /// </summary>
    internal const int SharedSecretSize = 32;

    /// <summary>
    /// Runs ML-KEM.KeyGen_internal (FIPS 203 Algorithm 16) from the two 32-byte seeds, producing the encoded
    /// encapsulation and decapsulation keys.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="d">The 32-byte K-PKE key-generation seed.</param>
    /// <param name="z">The 32-byte implicit-rejection seed.</param>
    /// <param name="encapsulationKey">The span receiving the 384k + 32 byte encapsulation key.</param>
    /// <param name="decapsulationKey">The span receiving the 768k + 96 byte decapsulation key.</param>
    /// <exception cref="ArgumentException">A span does not have its exact required length.</exception>
    internal static void KeyGen(
        MLKemParameters parameters,
        ReadOnlySpan<byte> d,
        ReadOnlySpan<byte> z,
        Span<byte> encapsulationKey,
        Span<byte> decapsulationKey)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(d, 32);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(z, 32);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(encapsulationKey, parameters.EncapsulationKeySize);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(decapsulationKey, parameters.DecapsulationKeySize);

        int k = parameters.K;
        int pkeSecretSize = 384 * k;

        // dk = dk_PKE ‖ ek ‖ H(ek) ‖ z.
        Span<byte> dkPke = decapsulationKey[..pkeSecretSize];
        Span<byte> ekCopy = decapsulationKey.Slice(pkeSecretSize, parameters.EncapsulationKeySize);
        Span<byte> ekHash = decapsulationKey.Slice(pkeSecretSize + parameters.EncapsulationKeySize, 32);
        Span<byte> zCopy = decapsulationKey[(pkeSecretSize + parameters.EncapsulationKeySize + 32)..];

        PkeKeyGen(parameters, d, encapsulationKey, dkPke);
        encapsulationKey.CopyTo(ekCopy);
        KeccakSponge.Sha3_256(encapsulationKey, ekHash);
        z.CopyTo(zCopy);
    }

    /// <summary>
    /// Runs ML-KEM.Encaps_internal (FIPS 203 Algorithm 17) with explicit encapsulation randomness, producing the
    /// ciphertext and shared secret.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="encapsulationKey">The encoded encapsulation key. Assumed already validated.</param>
    /// <param name="m">
    /// The 32-byte encapsulation randomness; supplied explicitly so known-answer tests can drive it.
    /// </param>
    /// <param name="ciphertext">The span receiving the ciphertext.</param>
    /// <param name="sharedSecret">The span receiving the 32-byte shared secret.</param>
    /// <exception cref="ArgumentException">A span does not have its exact required length.</exception>
    internal static void Encapsulate(
        MLKemParameters parameters,
        ReadOnlySpan<byte> encapsulationKey,
        ReadOnlySpan<byte> m,
        Span<byte> ciphertext,
        Span<byte> sharedSecret)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(encapsulationKey, parameters.EncapsulationKeySize);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(m, 32);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(ciphertext, parameters.CiphertextSize);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(sharedSecret, SharedSecretSize);

        // (K, r) = G(m ‖ H(ek)).
        Span<byte> ekHash = stackalloc byte[32];
        KeccakSponge.Sha3_256(encapsulationKey, ekHash);

        Span<byte> kr = stackalloc byte[64];
        KeccakSponge.Sha3_512(m, ekHash, kr);

        PkeEncrypt(parameters, encapsulationKey, m, kr[32..], ciphertext);
        kr[..32].CopyTo(sharedSecret);

        CryptographyHelper.Clear(kr);
    }

    /// <summary>
    /// Runs ML-KEM.Decaps_internal (FIPS 203 Algorithm 18), recovering the shared secret with implicit rejection: a
    /// tampered ciphertext yields the unrelated key J(z ‖ c) rather than an error.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="decapsulationKey">The encoded decapsulation key. Assumed already validated.</param>
    /// <param name="ciphertext">The candidate ciphertext.</param>
    /// <param name="sharedSecret">The span receiving the 32-byte shared secret.</param>
    /// <exception cref="ArgumentException">A span does not have its exact required length.</exception>
    internal static void Decapsulate(
        MLKemParameters parameters,
        ReadOnlySpan<byte> decapsulationKey,
        ReadOnlySpan<byte> ciphertext,
        Span<byte> sharedSecret)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(decapsulationKey, parameters.DecapsulationKeySize);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(ciphertext, parameters.CiphertextSize);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(sharedSecret, SharedSecretSize);

        int k = parameters.K;
        int pkeSecretSize = 384 * k;

        ReadOnlySpan<byte> dkPke = decapsulationKey[..pkeSecretSize];
        ReadOnlySpan<byte> ek = decapsulationKey.Slice(pkeSecretSize, parameters.EncapsulationKeySize);
        ReadOnlySpan<byte> ekHash = decapsulationKey.Slice(pkeSecretSize + parameters.EncapsulationKeySize, 32);
        ReadOnlySpan<byte> z = decapsulationKey[(pkeSecretSize + parameters.EncapsulationKeySize + 32)..];

        Span<byte> mPrime = stackalloc byte[32];
        PkeDecrypt(parameters, dkPke, ciphertext, mPrime);

        // (K', r') = G(m' ‖ H(ek)); K̄ = J(z ‖ c).
        Span<byte> kr = stackalloc byte[64];
        KeccakSponge.Sha3_512(mPrime, ekHash, kr);

        Span<byte> rejectionKey = stackalloc byte[SharedSecretSize];
        KeccakSponge.Shake256(z, ciphertext, rejectionKey);

        // Re-encrypt and select K' or K̄ without a data-dependent branch.
        byte[] ciphertextPrime = new byte[parameters.CiphertextSize];
        PkeEncrypt(parameters, ek, mPrime, kr[32..], ciphertextPrime);

        int difference = CryptographyHelper.ConstantTimeDifference(ciphertext, ciphertextPrime);
        CryptographyHelper.ConstantTimeSelect(difference, kr[..32], rejectionKey, sharedSecret);

        CryptographyHelper.Clear(mPrime);
        CryptographyHelper.Clear(kr);
        CryptographyHelper.Clear(rejectionKey);
        CryptographyHelper.Clear(ciphertextPrime);
    }

    /// <summary>
    /// Performs the FIPS 203 §7.2 encapsulation-key check: the encoded coefficients must round-trip through the 12-bit
    /// codec, that is, every value must already be reduced modulo q.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="encapsulationKey">The candidate encapsulation key.</param>
    /// <returns><see langword="true" /> when the key is well-formed; otherwise, <see langword="false" />.</returns>
    internal static bool ValidateEncapsulationKey(MLKemParameters parameters, ReadOnlySpan<byte> encapsulationKey)
    {
        if (encapsulationKey.Length != parameters.EncapsulationKeySize)
            return false;

        // Decode the 12-bit coefficients and reject any value at or above q.
        int coefficientBytes = 384 * parameters.K;
        for (int offset = 0; offset < coefficientBytes; offset += 3)
        {
            byte b0 = encapsulationKey[offset];
            byte b1 = encapsulationKey[offset + 1];
            byte b2 = encapsulationKey[offset + 2];

            int first = b0 | ((b1 & 0x0F) << 8);
            int second = (b1 >> 4) | (b2 << 4);

            if (first >= Q || second >= Q)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Performs the FIPS 203 §7.3 decapsulation-key check: the embedded encapsulation key must be well-formed and its
    /// stored hash must equal H(ek).
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="decapsulationKey">The candidate decapsulation key.</param>
    /// <returns><see langword="true" /> when the key is well-formed; otherwise, <see langword="false" />.</returns>
    internal static bool ValidateDecapsulationKey(MLKemParameters parameters, ReadOnlySpan<byte> decapsulationKey)
    {
        if (decapsulationKey.Length != parameters.DecapsulationKeySize)
            return false;

        int pkeSecretSize = 384 * parameters.K;
        ReadOnlySpan<byte> ek = decapsulationKey.Slice(pkeSecretSize, parameters.EncapsulationKeySize);
        ReadOnlySpan<byte> storedHash = decapsulationKey.Slice(pkeSecretSize + parameters.EncapsulationKeySize, 32);

        Span<byte> actualHash = stackalloc byte[32];
        KeccakSponge.Sha3_256(ek, actualHash);

        return CryptographicOperationsFixedTimeEquals(storedHash, actualHash) && ValidateEncapsulationKey(parameters, ek);
    }

    /// <summary>
    /// Runs K-PKE.KeyGen (FIPS 203 Algorithm 13), producing the encoded public key (with the matrix seed appended) and
    /// the encoded secret vector.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="d">The 32-byte seed.</param>
    /// <param name="ekPke">The span receiving ByteEncode₁₂(t̂) ‖ ρ.</param>
    /// <param name="dkPke">The span receiving ByteEncode₁₂(ŝ).</param>
    private static void PkeKeyGen(MLKemParameters parameters, ReadOnlySpan<byte> d, Span<byte> ekPke, Span<byte> dkPke)
    {
        int k = parameters.K;

        // (ρ, σ) = G(d ‖ k) with the rank appended as a single byte (FIPS 203 final).
        Span<byte> rhoSigma = stackalloc byte[64];
        Span<byte> rankByte = stackalloc byte[1];
        rankByte[0] = (byte)k;
        KeccakSponge.Sha3_512(d, rankByte, rhoSigma);

        ReadOnlySpan<byte> rho = rhoSigma[..32];
        ReadOnlySpan<byte> sigma = rhoSigma[32..];

        int[][] sHat = new int[k][];
        int[][] tHat = new int[k][];

        // ŝ = NTT(s) with s[i] ← CBD_η₁(PRF(σ, i)).
        for (int i = 0; i < k; i++)
        {
            sHat[i] = new int[N];
            SamplePolyCbd(parameters.Eta1, sigma, (byte)i, sHat[i]);
            Ntt(sHat[i]);
        }

        // t̂[i] = Σⱼ Â[i][j] ∘ ŝ[j] + NTT(e[i]) with e[i] ← CBD_η₁(PRF(σ, k + i)).
        int[] aRow = new int[N];
        int[] product = new int[N];
        for (int i = 0; i < k; i++)
        {
            tHat[i] = new int[N];
            SamplePolyCbd(parameters.Eta1, sigma, (byte)(k + i), tHat[i]);
            Ntt(tHat[i]);

            for (int j = 0; j < k; j++)
            {
                SampleNtt(rho, (byte)j, (byte)i, aRow);
                MultiplyNtt(aRow, sHat[j], product);
                AddInto(tHat[i], product);
            }
        }

        for (int i = 0; i < k; i++)
        {
            ByteEncode(12, tHat[i], ekPke.Slice(i * 384, 384));
            ByteEncode(12, sHat[i], dkPke.Slice(i * 384, 384));
        }

        rho.CopyTo(ekPke[(384 * k)..]);

        CryptographyHelper.Clear(rhoSigma);
        foreach (int[] poly in sHat)
            CryptographyHelper.Clear(poly);
    }

    /// <summary>
    /// Runs K-PKE.Encrypt (FIPS 203 Algorithm 14), producing the compressed ciphertext for a 32-byte message under the
    /// given encryption randomness.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="ekPke">The encoded public key ByteEncode₁₂(t̂) ‖ ρ.</param>
    /// <param name="m">The 32-byte message.</param>
    /// <param name="r">The 32-byte encryption randomness seed.</param>
    /// <param name="ciphertext">The span receiving c₁ ‖ c₂.</param>
    private static void PkeEncrypt(
        MLKemParameters parameters,
        ReadOnlySpan<byte> ekPke,
        ReadOnlySpan<byte> m,
        ReadOnlySpan<byte> r,
        Span<byte> ciphertext)
    {
        int k = parameters.K;
        ReadOnlySpan<byte> rho = ekPke[(384 * k)..];

        // ŷ[i] = NTT(y[i]) with y[i] ← CBD_η₁(PRF(r, i)).
        int[][] yHat = new int[k][];
        for (int i = 0; i < k; i++)
        {
            yHat[i] = new int[N];
            SamplePolyCbd(parameters.Eta1, r, (byte)i, yHat[i]);
            Ntt(yHat[i]);
        }

        int[] aColumn = new int[N];
        int[] product = new int[N];
        int[] u = new int[N];
        int[] noise = new int[N];

        // u[i] = InvNTT(Σⱼ Âᵀ[i][j] ∘ ŷ[j]) + e₁[i]; Âᵀ[i][j] = Â[j][i] is sampled with the indices (i, j).
        for (int i = 0; i < k; i++)
        {
            Array.Clear(u);
            for (int j = 0; j < k; j++)
            {
                SampleNtt(rho, (byte)i, (byte)j, aColumn);
                MultiplyNtt(aColumn, yHat[j], product);
                AddInto(u, product);
            }

            InvNtt(u);
            SamplePolyCbd(parameters.Eta2, r, (byte)(k + i), noise);
            AddInto(u, noise);

            CompressEncode(parameters.Du, u, ciphertext.Slice(i * 32 * parameters.Du, 32 * parameters.Du));
        }

        // v = InvNTT(t̂ᵀ ∘ ŷ) + e₂ + Decompress₁(ByteDecode₁(m)).
        int[] v = new int[N];
        int[] tRow = new int[N];
        for (int i = 0; i < k; i++)
        {
            ByteDecode(12, ekPke.Slice(i * 384, 384), tRow);
            MultiplyNtt(tRow, yHat[i], product);
            AddInto(v, product);
        }

        InvNtt(v);
        SamplePolyCbd(parameters.Eta2, r, (byte)(2 * k), noise);
        AddInto(v, noise);

        int[] message = new int[N];
        ByteDecode(1, m, message);
        for (int i = 0; i < N; i++)
            v[i] = (v[i] + Decompress(1, message[i])) % Q;

        CompressEncode(parameters.Dv, v, ciphertext[(k * 32 * parameters.Du)..]);

        foreach (int[] poly in yHat)
            CryptographyHelper.Clear(poly);
        CryptographyHelper.Clear(v);
        CryptographyHelper.Clear(noise);
        CryptographyHelper.Clear(message);
    }

    /// <summary>
    /// Runs K-PKE.Decrypt (FIPS 203 Algorithm 15), recovering the 32-byte message from a ciphertext.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="dkPke">The encoded secret vector ByteEncode₁₂(ŝ).</param>
    /// <param name="ciphertext">The ciphertext c₁ ‖ c₂.</param>
    /// <param name="m">The 32-byte span receiving the recovered message.</param>
    private static void PkeDecrypt(
        MLKemParameters parameters,
        ReadOnlySpan<byte> dkPke,
        ReadOnlySpan<byte> ciphertext,
        Span<byte> m)
    {
        int k = parameters.K;

        // w = v' − InvNTT(ŝᵀ ∘ NTT(u')).
        int[] w = new int[N];
        int[] u = new int[N];
        int[] s = new int[N];
        int[] product = new int[N];

        for (int i = 0; i < k; i++)
        {
            DecodeDecompress(parameters.Du, ciphertext.Slice(i * 32 * parameters.Du, 32 * parameters.Du), u);
            Ntt(u);
            ByteDecode(12, dkPke.Slice(i * 384, 384), s);
            MultiplyNtt(s, u, product);
            AddInto(w, product);
        }

        InvNtt(w);

        int[] v = new int[N];
        DecodeDecompress(parameters.Dv, ciphertext[(k * 32 * parameters.Du)..], v);

        int[] message = new int[N];
        for (int i = 0; i < N; i++)
            message[i] = Compress(1, (v[i] - w[i] + (Q * 2)) % Q);

        ByteEncode(1, message, m);

        CryptographyHelper.Clear(w);
        CryptographyHelper.Clear(s);
        CryptographyHelper.Clear(message);
    }

    /// <summary>
    /// Adds <paramref name="source" /> into <paramref name="accumulator" /> coefficient-wise modulo q.
    /// </summary>
    /// <param name="accumulator">The polynomial updated in place. Coefficients in [0, q).</param>
    /// <param name="source">The polynomial to add. Coefficients in [0, q).</param>
    private static void AddInto(Span<int> accumulator, ReadOnlySpan<int> source)
    {
        for (int i = 0; i < N; i++)
            accumulator[i] = (accumulator[i] + source[i]) % Q;
    }

    /// <summary>
    /// Compares two spans for equality in fixed time via
    /// <see cref="System.Security.Cryptography.CryptographicOperations.FixedTimeEquals" />.
    /// </summary>
    /// <param name="left">The first span.</param>
    /// <param name="right">The second span.</param>
    /// <returns><see langword="true" /> when the spans are equal; otherwise, <see langword="false" />.</returns>
    private static bool CryptographicOperationsFixedTimeEquals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right) =>
        System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(left, right);
}
