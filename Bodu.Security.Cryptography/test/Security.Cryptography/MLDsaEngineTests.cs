// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="MLDsaEngine" /> arithmetic: the modular reductions, the transforms, the
/// coefficient-wise products and the decomposition, held to <see cref="MLDsaReference" /> and to plain integer
/// arithmetic.
/// </summary>
[TestClass]
public partial class MLDsaEngineTests
{
    /// <summary>The coefficient modulus q = 8380417.</summary>
    private const int Q = MLDsaEngine.Q;

    /// <summary>γ₂ = (q − 1) / 32, the parameter of ML-DSA-65 and ML-DSA-87.</summary>
    private const int WideGamma2 = (Q - 1) / 32;

    /// <summary>γ₂ = (q − 1) / 88, the parameter of ML-DSA-44.</summary>
    private const int NarrowGamma2 = (Q - 1) / 88;

    /// <summary>
    /// Returns the representative of a value modulo q in [0, q).
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The value modulo q.</returns>
    private static int Mod(long value) =>
        (int)(((value % Q) + Q) % Q);

    /// <summary>
    /// Returns the engine parameters for a parameter-set designator.
    /// </summary>
    /// <param name="designator">The designator: 44, 65 or 87.</param>
    /// <returns>The parameter set.</returns>
    private static MLDsaParameters Parameters(int designator) =>
        designator switch
        {
            44 => MLDsaParameters.MLDsa44,
            65 => MLDsaParameters.MLDsa65,
            _ => MLDsaParameters.MLDsa87,
        };

    /// <summary>
    /// Generates a seeded key pair through the key generation that keeps no values.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="seed">The 32-byte seed ξ.</param>
    /// <returns>The encoded public and private keys.</returns>
    private static (byte[] PublicKey, byte[] PrivateKey) GenerateKeys(MLDsaParameters parameters, byte[] seed)
    {
        byte[] publicKey = new byte[parameters.PublicKeySize];
        byte[] privateKey = new byte[parameters.PrivateKeySize];
        MLDsaEngine.KeyGen(parameters, seed, publicKey, privateKey);

        return (publicKey, privateKey);
    }

    /// <summary>
    /// Returns a seed ξ drawn from a seeded generator.
    /// </summary>
    /// <param name="seed">The generator's seed.</param>
    /// <returns>The 32-byte seed ξ.</returns>
    private static byte[] Seed(int seed)
    {
        byte[] xi = new byte[32];
        new Random(seed).NextBytes(xi);

        return xi;
    }

    /// <summary>
    /// Derives from encoded keys, through the Expand methods, the values a key keeps for signing and verification.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="publicKey">The encoded public key.</param>
    /// <param name="privateKey">The encoded private key.</param>
    /// <returns>The values: Â, NTT(t₁·2ᵈ), ŝ₁, ŝ₂ and t̂₀.</returns>
    private static KeyValues Expand(MLDsaParameters parameters, byte[] publicKey, byte[] privateKey)
    {
        KeyValues values = KeyValues.Allocate(parameters);
        MLDsaEngine.ExpandMatrix(parameters, publicKey.AsSpan(0, 32), values.Matrix);
        MLDsaEngine.ExpandPublicKey(parameters, publicKey, values.HighOrderVector);
        MLDsaEngine.ExpandPrivateKey(parameters, privateKey, values.SecretVector1, values.SecretVector2, values.LowOrderVector);

        return values;
    }

    /// <summary>
    /// Returns polynomials whose coefficients sit at the ends of [0, q) and at a few patterns, then seeded ones.
    /// </summary>
    /// <returns>The polynomials.</returns>
    private static IEnumerable<int[]> Polynomials()
    {
        yield return new int[MLDsaEngine.N];
        yield return Enumerable.Repeat(Q - 1, MLDsaEngine.N).ToArray();
        yield return Enumerable.Range(0, MLDsaEngine.N).Select(i => i % 2 == 0 ? Q - 1 : 0).ToArray();
        yield return Enumerable.Range(0, MLDsaEngine.N).Select(i => i == 0 ? 1 : 0).ToArray();
        yield return Enumerable.Range(0, MLDsaEngine.N).Select(i => i == MLDsaEngine.N - 1 ? Q - 1 : 0).ToArray();

        var random = new Random(0x0204_0001);
        for (int iteration = 0; iteration < 200; iteration++)
            yield return Enumerable.Range(0, MLDsaEngine.N).Select(_ => random.Next(Q)).ToArray();
    }

    /// <summary>
    /// Returns polynomials whose coefficients sit at the ends of (−q, q) - all q − 1, all −(q − 1), and the two signs
    /// alternating in runs of every power of two from 1 to 128, which pairs them differently in each butterfly layer -
    /// then seeded ones.
    /// </summary>
    /// <returns>The polynomials.</returns>
    private static IEnumerable<int[]> SignedPolynomials()
    {
        yield return Enumerable.Repeat(Q - 1, MLDsaEngine.N).ToArray();
        yield return Enumerable.Repeat(-(Q - 1), MLDsaEngine.N).ToArray();

        for (int shift = 0; shift < 8; shift++)
            yield return Enumerable.Range(0, MLDsaEngine.N).Select(i => ((i >> shift) & 1) == 0 ? Q - 1 : -(Q - 1)).ToArray();

        var random = new Random(0x0204_0006);
        for (int iteration = 0; iteration < 200; iteration++)
            yield return Enumerable.Range(0, MLDsaEngine.N).Select(_ => random.Next(-Q + 1, Q)).ToArray();
    }

    /// <summary>
    /// Returns the coefficients around the decomposition's boundaries for a value of γ₂: the ends of [0, q), either
    /// side of (q − 1) / 2, and either side of every multiple of 2γ₂ and of every midpoint between two.
    /// </summary>
    /// <param name="gamma2">The parameter γ₂.</param>
    /// <returns>The coefficients, each in [0, q).</returns>
    private static IEnumerable<int> BoundaryCoefficients(int gamma2)
    {
        IEnumerable<int> multiples = Enumerable.Range(0, (Q / (2 * gamma2)) + 1)
            .SelectMany(m => new[] { (2 * gamma2 * m) - 1, 2 * gamma2 * m, (2 * gamma2 * m) + 1, (2 * gamma2 * m) + gamma2, (2 * gamma2 * m) + gamma2 + 1 });

        return new[] { 0, 1, ((Q - 1) / 2) - 1, (Q - 1) / 2, (Q + 1) / 2, Q - 2, Q - 1 }
            .Concat(multiples)
            .Where(value => value >= 0 && value < Q);
    }

    /// <summary>
    /// Packs a sequence of coefficients into polynomials, 256 at a time, padding the last with zeros.
    /// </summary>
    /// <param name="coefficients">The coefficients.</param>
    /// <returns>The polynomials.</returns>
    private static IEnumerable<int[]> AsPolynomials(IEnumerable<int> coefficients)
    {
        foreach (int[] chunk in coefficients.Chunk(MLDsaEngine.N))
        {
            int[] polynomial = new int[MLDsaEngine.N];
            chunk.CopyTo(polynomial, 0);
            yield return polynomial;
        }
    }

    /// <summary>
    /// Returns the edge, patterned and seeded polynomials of <see cref="Polynomials" />, each paired with a second one
    /// drawn from the same set.
    /// </summary>
    /// <returns>The pairs.</returns>
    private static IEnumerable<(int[] Left, int[] Right)> PolynomialPairs()
    {
        int[][] polynomials = Polynomials().ToArray();
        for (int i = 0; i < polynomials.Length; i++)
            yield return (polynomials[i], polynomials[((i * 7) + 1) % polynomials.Length]);
    }

    /// <summary>
    /// Asserts that a method of the vector kernel forbids inlining and is aggressively optimized, so it is compiled on
    /// its own, with its own inlining budget, whatever dynamic PGO makes of the dispatcher.
    /// </summary>
    /// <param name="name">The method's name; of its overloads, the entry point, which takes coefficients by reference.</param>
    private static void AssertKernelIsCompiledOnItsOwn(string name)
    {
        MethodInfo kernel = typeof(MLDsaEngine.Vector256Kernel)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Single(method => method.Name == name && method.GetParameters().Any(parameter => parameter.ParameterType.IsByRef));

        Assert.IsTrue(kernel.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoInlining), name);
        Assert.IsTrue(kernel.MethodImplementationFlags.HasFlag(MethodImplAttributes.AggressiveOptimization), name);
    }

    /// <summary>
    /// Parses a kernel's name, reporting the test inconclusive when the processor cannot run the kernel.
    /// </summary>
    /// <param name="name">The kernel's name.</param>
    /// <returns>The kernel.</returns>
    private static MLDsaEngine.KernelKind ParseSupportedKernel(string name)
    {
        MLDsaEngine.KernelKind kernel = Enum.Parse<MLDsaEngine.KernelKind>(name);
        if (!MLDsaEngine.IsSupported(kernel))
            Assert.Inconclusive($"The {name} kernel cannot run on this processor.");

        return kernel;
    }
}
