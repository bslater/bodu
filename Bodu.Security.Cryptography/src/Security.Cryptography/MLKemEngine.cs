// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemEngine.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the ML-KEM key-encapsulation mechanism of NIST FIPS 203: module-lattice key generation, the K-PKE
/// encryption core, and the Fujisaki-Okamoto encapsulate/decapsulate transform with implicit rejection.
/// </summary>
/// <remarks>
/// <para>
/// Polynomials are held as 256 <see cref="int" /> coefficients in [0, q) with q = 3329 between operations. Within the
/// transforms and products, coefficients are reduced by Montgomery and Barrett reduction (see the Reduction partial):
/// fixed sequences of multiplications, shifts and masks, never a division, so their timing does not depend on the
/// values. The decapsulation re-encryption comparison and the secret selection between the real and implicit-rejection
/// keys are byte-wise constant-time.
/// </para>
/// </remarks>
internal static partial class MLKemEngine
{
    /// <summary>The polynomial degree n = 256 shared by every parameter set.</summary>
    internal const int N = 256;

    /// <summary>The coefficient modulus q = 3329.</summary>
    internal const int Q = 3329;

    /// <summary>The size, in bytes, of the shared secret produced by encapsulation and decapsulation.</summary>
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
        Span<byte> decapsulationKey) =>
        KeyGen(parameters, d, z, encapsulationKey, decapsulationKey, [], [], []);

    /// <summary>
    /// Runs ML-KEM.KeyGen_internal (FIPS 203 Algorithm 16) from the two 32-byte seeds, producing the encoded
    /// encapsulation and decapsulation keys and, optionally, the matrix Â and the vectors t̂ and ŝ that key generation
    /// computes along the way.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="d">The 32-byte K-PKE key-generation seed.</param>
    /// <param name="z">The 32-byte implicit-rejection seed.</param>
    /// <param name="encapsulationKey">The span receiving the 384k + 32 byte encapsulation key.</param>
    /// <param name="decapsulationKey">The span receiving the 768k + 96 byte decapsulation key.</param>
    /// <param name="matrix">
    /// The span receiving Â, laid out as <see cref="ExpandMatrix" /> lays it out, or an empty span to discard it.
    /// </param>
    /// <param name="publicVector">The span receiving t̂, or an empty span to discard it.</param>
    /// <param name="secretVector">The span receiving ŝ, or an empty span to discard it.</param>
    /// <exception cref="ArgumentException">A span does not have its exact required length.</exception>
    internal static void KeyGen(
        MLKemParameters parameters,
        ReadOnlySpan<byte> d,
        ReadOnlySpan<byte> z,
        Span<byte> encapsulationKey,
        Span<byte> decapsulationKey,
        Span<int> matrix,
        Span<int> publicVector,
        Span<int> secretVector)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(d, 32);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(z, 32);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(encapsulationKey, parameters.EncapsulationKeySize);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(decapsulationKey, parameters.DecapsulationKeySize);
        if (!matrix.IsEmpty)
            ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(matrix, parameters.K * parameters.K * N);
        if (!publicVector.IsEmpty)
            ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(publicVector, parameters.K * N);
        if (!secretVector.IsEmpty)
            ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(secretVector, parameters.K * N);

        int k = parameters.K;
        int pkeSecretSize = 384 * k;

        // dk = dk_PKE ‖ ek ‖ H(ek) ‖ z.
        Span<byte> dkPke = decapsulationKey[..pkeSecretSize];
        Span<byte> ekCopy = decapsulationKey.Slice(pkeSecretSize, parameters.EncapsulationKeySize);
        Span<byte> ekHash = decapsulationKey.Slice(pkeSecretSize + parameters.EncapsulationKeySize, 32);
        Span<byte> zCopy = decapsulationKey[(pkeSecretSize + parameters.EncapsulationKeySize + 32)..];

        PkeKeyGen(parameters, d, encapsulationKey, dkPke, matrix, publicVector, secretVector);
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

        int k = parameters.K;
        int packedVectorSize = 384 * k;

        Span<byte> ekHash = stackalloc byte[32];
        KeccakSponge.Sha3_256(encapsulationKey, ekHash);

        int[] matrix = new int[k * k * N];
        ExpandMatrix(parameters, encapsulationKey[packedVectorSize..], matrix);

        Span<int> tHat = stackalloc int[k * N];
        DecodeVector(parameters, encapsulationKey[..packedVectorSize], tHat);

        Encapsulate(parameters, ekHash, matrix, tHat, m, ciphertext, sharedSecret);
    }

    /// <summary>
    /// Runs ML-KEM.Encaps_internal (FIPS 203 Algorithm 17) from the values derived from the encapsulation key - its
    /// hash, the matrix Â and the vector t̂ - so a caller that keeps them need not derive them again for each
    /// encapsulation.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="encapsulationKeyHash">The 32-byte hash H(ek).</param>
    /// <param name="matrix">The matrix Â, laid out as <see cref="ExpandMatrix" /> lays it out.</param>
    /// <param name="publicVector">
    /// The vector t̂ decoded from the encapsulation key, as <see cref="DecodeVector" /> decodes it.
    /// </param>
    /// <param name="m">The 32-byte encapsulation randomness.</param>
    /// <param name="ciphertext">The span receiving the ciphertext.</param>
    /// <param name="sharedSecret">The span receiving the 32-byte shared secret.</param>
    /// <exception cref="ArgumentException">A span does not have its exact required length.</exception>
    internal static void Encapsulate(
        MLKemParameters parameters,
        ReadOnlySpan<byte> encapsulationKeyHash,
        ReadOnlySpan<int> matrix,
        ReadOnlySpan<int> publicVector,
        ReadOnlySpan<byte> m,
        Span<byte> ciphertext,
        Span<byte> sharedSecret)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(encapsulationKeyHash, 32);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(matrix, parameters.K * parameters.K * N);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(publicVector, parameters.K * N);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(m, 32);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(ciphertext, parameters.CiphertextSize);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(sharedSecret, SharedSecretSize);

        // (K, r) = G(m ‖ H(ek)).
        Span<byte> kr = stackalloc byte[64];
        KeccakSponge.Sha3_512(m, encapsulationKeyHash, kr);

        PkeEncrypt(parameters, matrix, publicVector, m, kr[32..], ciphertext);
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

        int k = parameters.K;
        int pkeSecretSize = 384 * k;
        ReadOnlySpan<byte> ek = decapsulationKey.Slice(pkeSecretSize, parameters.EncapsulationKeySize);

        int[] matrix = new int[k * k * N];
        ExpandMatrix(parameters, ek[pkeSecretSize..], matrix);

        Span<int> tHat = stackalloc int[k * N];
        DecodeVector(parameters, ek[..pkeSecretSize], tHat);

        Span<int> sHat = stackalloc int[k * N];
        DecodeVector(parameters, decapsulationKey[..pkeSecretSize], sHat);

        Decapsulate(parameters, decapsulationKey, matrix, tHat, sHat, ciphertext, sharedSecret);

        CryptographyHelper.Clear(sHat);
    }

    /// <summary>
    /// Runs ML-KEM.Decaps_internal (FIPS 203 Algorithm 18) from the values derived from the decapsulation key - the
    /// matrix Â and the vectors t̂ and ŝ - so a caller that keeps them need not derive them again for each
    /// decapsulation.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="decapsulationKey">The encoded decapsulation key, which supplies H(ek) and z.</param>
    /// <param name="matrix">The matrix Â, laid out as <see cref="ExpandMatrix" /> lays it out.</param>
    /// <param name="publicVector">The vector t̂ decoded from the embedded encapsulation key.</param>
    /// <param name="secretVector">The secret vector ŝ decoded from the decapsulation key.</param>
    /// <param name="ciphertext">The candidate ciphertext.</param>
    /// <param name="sharedSecret">The span receiving the 32-byte shared secret.</param>
    /// <exception cref="ArgumentException">A span does not have its exact required length.</exception>
    internal static void Decapsulate(
        MLKemParameters parameters,
        ReadOnlySpan<byte> decapsulationKey,
        ReadOnlySpan<int> matrix,
        ReadOnlySpan<int> publicVector,
        ReadOnlySpan<int> secretVector,
        ReadOnlySpan<byte> ciphertext,
        Span<byte> sharedSecret)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(decapsulationKey, parameters.DecapsulationKeySize);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(matrix, parameters.K * parameters.K * N);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(publicVector, parameters.K * N);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(secretVector, parameters.K * N);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(ciphertext, parameters.CiphertextSize);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(sharedSecret, SharedSecretSize);

        int pkeSecretSize = 384 * parameters.K;
        ReadOnlySpan<byte> ekHash = decapsulationKey.Slice(pkeSecretSize + parameters.EncapsulationKeySize, 32);
        ReadOnlySpan<byte> z = decapsulationKey[(pkeSecretSize + parameters.EncapsulationKeySize + 32)..];

        Span<byte> mPrime = stackalloc byte[32];
        PkeDecrypt(parameters, secretVector, ciphertext, mPrime);

        // (K', r') = G(m' ‖ H(ek)); K̄ = J(z ‖ c).
        Span<byte> kr = stackalloc byte[64];
        KeccakSponge.Sha3_512(mPrime, ekHash, kr);

        Span<byte> rejectionKey = stackalloc byte[SharedSecretSize];
        KeccakSponge.Shake256(z, ciphertext, rejectionKey);

        // Re-encrypt and select K' or K̄ without a data-dependent branch.
        Span<byte> ciphertextPrime = stackalloc byte[parameters.CiphertextSize];
        PkeEncrypt(parameters, matrix, publicVector, mPrime, kr[32..], ciphertextPrime);

        int difference = CryptographyHelper.ConstantTimeDifference(ciphertext, ciphertextPrime);
        CryptographyHelper.ConstantTimeSelect(difference, kr[..32], rejectionKey, sharedSecret);

        CryptographyHelper.Clear(mPrime);
        CryptographyHelper.Clear(kr);
        CryptographyHelper.Clear(rejectionKey);
        CryptographyHelper.Clear(ciphertextPrime);
    }

    /// <summary>
    /// Expands the matrix Â from the seed ρ (FIPS 203 Algorithm 13, lines 3-7): entry (i, j) is SampleNTT(ρ ‖ j ‖ i).
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="rho">The 32-byte matrix seed, the last 32 bytes of the encapsulation key.</param>
    /// <param name="matrix">
    /// The span receiving the k² NTT-domain polynomials, entry (i, j) at offset (i·k + j)·256.
    /// </param>
    /// <exception cref="ArgumentException">A span does not have its exact required length.</exception>
    internal static void ExpandMatrix(MLKemParameters parameters, ReadOnlySpan<byte> rho, Span<int> matrix)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(rho, 32);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(matrix, parameters.K * parameters.K * N);

        SampleMatrix(parameters, rho, matrix);
    }

    /// <summary>
    /// Decodes a vector of k polynomials packed 12 bits to the coefficient (ByteDecode₁₂ of each 384-byte block), as
    /// the encapsulation key packs t̂ and the decapsulation key packs ŝ.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="encoded">The 384k packed bytes.</param>
    /// <param name="vector">The span receiving the k·256 coefficients.</param>
    /// <exception cref="ArgumentException">A span does not have its exact required length.</exception>
    internal static void DecodeVector(MLKemParameters parameters, ReadOnlySpan<byte> encoded, Span<int> vector)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(encoded, 384 * parameters.K);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(vector, parameters.K * N);

        for (int i = 0; i < parameters.K; i++)
            ByteDecode(12, encoded.Slice(i * 384, 384), vector.Slice(i * N, N));
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
    /// <param name="matrix">
    /// The span receiving Â, or an empty span to sample each entry into scratch and discard it.
    /// </param>
    /// <param name="publicVector">The span receiving t̂, or an empty span to discard it.</param>
    /// <param name="secretVector">The span receiving ŝ, or an empty span to discard it.</param>
    private static void PkeKeyGen(
        MLKemParameters parameters,
        ReadOnlySpan<byte> d,
        Span<byte> ekPke,
        Span<byte> dkPke,
        Span<int> matrix,
        Span<int> publicVector,
        Span<int> secretVector)
    {
        int k = parameters.K;

        // (ρ, σ) = G(d ‖ k) with the rank appended as a single byte (FIPS 203 final).
        Span<byte> rhoSigma = stackalloc byte[64];
        Span<byte> rankByte = stackalloc byte[1];
        rankByte[0] = (byte)k;
        KeccakSponge.Sha3_512(d, rankByte, rhoSigma);

        ReadOnlySpan<byte> rho = rhoSigma[..32];
        ReadOnlySpan<byte> sigma = rhoSigma[32..];

        // The per-rank secret (ŝ) and public (t̂) vectors are held as flat stack workspaces (k ≤ 4, so k·N ≤ 1024
        // ints) rather than jagged heap arrays, keeping the secret vector off the managed heap for its whole lifetime.
        // They are adjacent, so their noise is sampled as one vector: s[i] ← CBD_η₁(PRF(σ, i)) and
        // e[i] ← CBD_η₁(PRF(σ, k + i)).
        Span<int> noise = stackalloc int[2 * k * N];
        Span<int> sHat = noise[..(k * N)];
        Span<int> tHat = noise[(k * N)..];
        SampleNoiseVector(parameters.Eta1, sigma, 0, noise);

        // ŝ = NTT(s), and NTT(e) in t̂.
        for (int offset = 0; offset < noise.Length; offset += N)
            Ntt(noise.Slice(offset, N));

        // Â, into the caller's span, or into a pooled one when the caller does not keep it; it is public either way.
        int[]? rentedMatrix = null;
        if (matrix.IsEmpty)
        {
            rentedMatrix = ArrayPool<int>.Shared.Rent(k * k * N);
            matrix = rentedMatrix.AsSpan(0, k * k * N);
        }

        SampleMatrix(parameters, rho, matrix);

        // t̂[i] = Σⱼ Â[i][j] ∘ ŝ[j] + NTT(e[i]).
        Span<int> product = stackalloc int[N];
        for (int i = 0; i < k; i++)
        {
            Span<int> tHatI = tHat.Slice(i * N, N);
            for (int j = 0; j < k; j++)
            {
                MultiplyNtt(matrix.Slice(((i * k) + j) * N, N), sHat.Slice(j * N, N), product);
                AddInto(tHatI, product);
            }
        }

        if (rentedMatrix is not null)
            ArrayPool<int>.Shared.Return(rentedMatrix);

        for (int i = 0; i < k; i++)
        {
            ByteEncode(12, tHat.Slice(i * N, N), ekPke.Slice(i * 384, 384));
            ByteEncode(12, sHat.Slice(i * N, N), dkPke.Slice(i * 384, 384));
        }

        rho.CopyTo(ekPke[(384 * k)..]);

        if (!publicVector.IsEmpty)
            tHat.CopyTo(publicVector);
        if (!secretVector.IsEmpty)
            sHat.CopyTo(secretVector);

        // Zero every scratch buffer before returning: sHat is the secret vector, and product/tHat are derived from it.
        // tHat is ultimately public, but clearing it too keeps a single uniform rule for the method.
        CryptographyHelper.Clear(rhoSigma);
        CryptographyHelper.Clear(product);
        CryptographyHelper.Clear(noise);
    }

    /// <summary>
    /// Runs K-PKE.Encrypt (FIPS 203 Algorithm 14), producing the compressed ciphertext for a 32-byte message under the
    /// given encryption randomness.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="matrix">The matrix Â, laid out as <see cref="ExpandMatrix" /> lays it out.</param>
    /// <param name="tHat">The decoded public vector t̂.</param>
    /// <param name="m">The 32-byte message.</param>
    /// <param name="r">The 32-byte encryption randomness seed.</param>
    /// <param name="ciphertext">The span receiving c₁ ‖ c₂.</param>
    private static void PkeEncrypt(
        MLKemParameters parameters,
        ReadOnlySpan<int> matrix,
        ReadOnlySpan<int> tHat,
        ReadOnlySpan<byte> m,
        ReadOnlySpan<byte> r,
        Span<byte> ciphertext)
    {
        int k = parameters.K;

        // ŷ[i] = NTT(y[i]) with y[i] ← CBD_η₁(PRF(r, i)). Held flat on the stack (k·N ≤ 1024 ints).
        Span<int> yHat = stackalloc int[k * N];
        SampleNoiseVector(parameters.Eta1, r, 0, yHat);
        for (int offset = 0; offset < yHat.Length; offset += N)
            Ntt(yHat.Slice(offset, N));

        // e₁[i] ← CBD_η₂(PRF(r, k + i)) and e₂ ← CBD_η₂(PRF(r, 2k)), sampled together.
        Span<int> noise = stackalloc int[(k + 1) * N];
        SampleNoiseVector(parameters.Eta2, r, k, noise);

        Span<int> product = stackalloc int[N];
        Span<int> u = stackalloc int[N];

        // u[i] = InvNTT(Σⱼ Âᵀ[i][j] ∘ ŷ[j]) + e₁[i], where Âᵀ[i][j] = Â[j][i].
        for (int i = 0; i < k; i++)
        {
            u.Clear();
            for (int j = 0; j < k; j++)
            {
                MultiplyNtt(matrix.Slice(((j * k) + i) * N, N), yHat.Slice(j * N, N), product);
                AddInto(u, product);
            }

            InvNtt(u);
            AddInto(u, noise.Slice(i * N, N));

            CompressEncode(parameters.Du, u, ciphertext.Slice(i * 32 * parameters.Du, 32 * parameters.Du));
        }

        // v = InvNTT(t̂ᵀ ∘ ŷ) + e₂ + Decompress₁(ByteDecode₁(m)).
        Span<int> v = stackalloc int[N];
        for (int i = 0; i < k; i++)
        {
            MultiplyNtt(tHat.Slice(i * N, N), yHat.Slice(i * N, N), product);
            AddInto(v, product);
        }

        InvNtt(v);
        AddInto(v, noise.Slice(k * N, N));

        Span<int> message = stackalloc int[N];
        ByteDecode(1, m, message);
        for (int i = 0; i < N; i++)
            v[i] = Canonicalize(v[i] + Decompress(1, message[i]) - Q);

        CompressEncode(parameters.Dv, v, ciphertext[(k * 32 * parameters.Du)..]);

        // yHat is the ephemeral encryption secret; product, u, and v are derived from it.
        CryptographyHelper.Clear(yHat);
        CryptographyHelper.Clear(product);
        CryptographyHelper.Clear(u);
        CryptographyHelper.Clear(v);
        CryptographyHelper.Clear(noise);
        CryptographyHelper.Clear(message);
    }

    /// <summary>
    /// Runs K-PKE.Decrypt (FIPS 203 Algorithm 15), recovering the 32-byte message from a ciphertext.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="sHat">The decoded secret vector ŝ.</param>
    /// <param name="ciphertext">The ciphertext c₁ ‖ c₂.</param>
    /// <param name="m">The 32-byte span receiving the recovered message.</param>
    private static void PkeDecrypt(
        MLKemParameters parameters,
        ReadOnlySpan<int> sHat,
        ReadOnlySpan<byte> ciphertext,
        Span<byte> m)
    {
        int k = parameters.K;

        // w = v' − InvNTT(ŝᵀ ∘ NTT(u')).
        Span<int> w = stackalloc int[N];
        Span<int> u = stackalloc int[N];
        Span<int> product = stackalloc int[N];

        for (int i = 0; i < k; i++)
        {
            DecodeDecompress(parameters.Du, ciphertext.Slice(i * 32 * parameters.Du, 32 * parameters.Du), u);
            Ntt(u);
            MultiplyNtt(sHat.Slice(i * N, N), u, product);
            AddInto(w, product);
        }

        InvNtt(w);

        Span<int> v = stackalloc int[N];
        DecodeDecompress(parameters.Dv, ciphertext[(k * 32 * parameters.Du)..], v);

        Span<int> message = stackalloc int[N];
        for (int i = 0; i < N; i++)
            message[i] = Compress(1, Canonicalize(v[i] - w[i]));

        ByteEncode(1, message, m);

        // w and product are derived from the secret vector; u and v come from the public ciphertext but are cleared too
        // so the method leaves no populated scratch behind.
        CryptographyHelper.Clear(w);
        CryptographyHelper.Clear(u);
        CryptographyHelper.Clear(product);
        CryptographyHelper.Clear(v);
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
            accumulator[i] = Canonicalize(accumulator[i] + source[i] - Q);
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
