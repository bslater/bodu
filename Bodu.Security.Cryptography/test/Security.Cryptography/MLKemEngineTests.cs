// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemEngineTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="MLKemEngine" /> arithmetic: the modular reductions, the transforms, the
/// base-case products and the binomial sampler, held to <see cref="MLKemReference" /> and to plain integer arithmetic.
/// </summary>
[TestClass]
public partial class MLKemEngineTests
{
    /// <summary>The coefficient modulus q = 3329.</summary>
    private const int Q = MLKemEngine.Q;

    /// <summary>
    /// Returns the representative of a value modulo q in [0, q).
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The value modulo q.</returns>
    private static int Mod(long value) =>
        (int)(((value % Q) + Q) % Q);

    /// <summary>
    /// Returns the engine parameters for a rank.
    /// </summary>
    /// <param name="k">The rank: 2, 3 or 4.</param>
    /// <returns>The parameter set.</returns>
    private static MLKemParameters Parameters(int k) =>
        k switch
        {
            2 => MLKemParameters.MLKem512,
            3 => MLKemParameters.MLKem768,
            _ => MLKemParameters.MLKem1024,
        };

    /// <summary>
    /// Returns polynomials whose coefficients sit at the ends of [0, q) and at a few patterns - among them q − 1 and 0
    /// alternating in runs of every power of two from 2 to 128, which pairs them differently in each butterfly layer -
    /// then seeded ones.
    /// </summary>
    /// <returns>The polynomials.</returns>
    private static IEnumerable<int[]> Polynomials()
    {
        yield return new int[MLKemEngine.N];
        yield return Enumerable.Repeat(Q - 1, MLKemEngine.N).ToArray();
        yield return Enumerable.Range(0, MLKemEngine.N).Select(i => i % 2 == 0 ? Q - 1 : 0).ToArray();
        yield return Enumerable.Range(0, MLKemEngine.N).Select(i => i == 0 ? 1 : 0).ToArray();
        yield return Enumerable.Range(0, MLKemEngine.N).Select(i => i == MLKemEngine.N - 1 ? Q - 1 : 0).ToArray();

        for (int shift = 1; shift < 8; shift++)
            yield return Enumerable.Range(0, MLKemEngine.N).Select(i => ((i >> shift) & 1) == 0 ? Q - 1 : 0).ToArray();

        var random = new Random(0x0203_0001);
        for (int iteration = 0; iteration < 200; iteration++)
            yield return Enumerable.Range(0, MLKemEngine.N).Select(_ => random.Next(Q)).ToArray();
    }

    /// <summary>
    /// Parses a kernel's name, reporting the test inconclusive when the processor cannot run the kernel.
    /// </summary>
    /// <param name="name">The kernel's name.</param>
    /// <returns>The kernel.</returns>
    private static MLKemEngine.KernelKind ParseSupportedKernel(string name)
    {
        MLKemEngine.KernelKind kernel = Enum.Parse<MLKemEngine.KernelKind>(name);
        if (!MLKemEngine.IsSupported(kernel))
            Assert.Inconclusive($"The {name} kernel cannot run on this processor.");

        return kernel;
    }
}
