// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.Reduce32.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that the sum reduction returns a representative within [−6283008, 6283008] for inputs at the ends of its
    /// range and seeded across it, and that freezing it lands in [0, q).
    /// </summary>
    [TestMethod]
    public void Reduce32_WhenValueIsWithinItsRange_ShouldReturnARepresentativeWithinBound()
    {
        const int Largest = int.MaxValue - (1 << 22);
        var random = new Random(0x0204_0004);
        IEnumerable<int> values = new[] { 0, 1, -1, Q, -Q, Largest, int.MinValue, Largest - 1, int.MinValue + 1 }
            .Concat(Enumerable.Range(0, 100_000).Select(_ => random.Next(int.MinValue, Largest)));

        foreach (int value in values)
        {
            int reduced = MLDsaEngine.Reduce32(value);
            int frozen = MLDsaEngine.Freeze(value);

            if (reduced < -6283008 || reduced > 6283008 || Mod(reduced) != Mod(value) || frozen != Mod(value))
                Assert.Fail($"Reduce32({value}) = {reduced}, Freeze({value}) = {frozen}.");
        }
    }

    /// <summary>
    /// Verifies that each kernel reduces every coefficient of a polynomial to the representative the coefficient
    /// reduction returns, for values at the ends of its range and seeded across it.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void Reduce32_WhenGivenAPolynomial_ForEachKernel_ShouldReduceEveryCoefficient(string kernel)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);
        const int Largest = int.MaxValue - (1 << 22);
        var random = new Random(0x0204_000C);
        IEnumerable<int> values = new[] { 0, 1, -1, Q, -Q, Largest, int.MinValue, Largest - 1, int.MinValue + 1 }
            .Concat(Enumerable.Range(0, 20_000).Select(_ => random.Next(int.MinValue, Largest)));

        foreach (int[] polynomial in AsPolynomials(values))
        {
            int[] reduced = (int[])polynomial.Clone();

            MLDsaEngine.Reduce32(kind, reduced);

            CollectionAssert.AreEqual(polynomial.Select(MLDsaEngine.Reduce32).ToArray(), reduced, $"{kernel}, from {polynomial[0]}");
        }
    }

    /// <summary>
    /// Verifies that reducing a polynomial without a named kernel reduces it as the kernel dispatch selects does.
    /// </summary>
    [TestMethod]
    public void Reduce32_WhenKernelIsNotNamed_ShouldMatchTheSelectedKernel()
    {
        foreach (int[] polynomial in SignedPolynomials().Take(20))
        {
            int[] expected = polynomial.Select(value => value * 200).ToArray();
            int[] actual = (int[])expected.Clone();

            MLDsaEngine.Reduce32(MLDsaEngine.SelectKernel(), expected);
            MLDsaEngine.Reduce32(actual.AsSpan());

            CollectionAssert.AreEqual(expected, actual);
        }
    }

    /// <summary>
    /// Verifies that reducing a polynomial rejects a span shorter than a polynomial with
    /// <see cref="ArgumentOutOfRangeException" /> naming it, rather than writing past its end, whichever kernel is
    /// named.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Auto")]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void Reduce32_WhenSpanIsShorterThanAPolynomial_ShouldThrowArgumentOutOfRangeException(string kernel)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            MLDsaEngine.Reduce32(kind, new int[MLDsaEngine.N - 1].AsSpan());
        });

        Assert.AreEqual("poly", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the vector kernel's reduction forbids inlining and is aggressively optimized.
    /// </summary>
    [TestMethod]
    public void Reduce32_WhenDeclared_ForTheVectorKernel_ShouldForbidInliningIntoTheDispatcher()
    {
        AssertKernelIsCompiledOnItsOwn(nameof(MLDsaEngine.Vector256Kernel.Reduce32));
    }
}
