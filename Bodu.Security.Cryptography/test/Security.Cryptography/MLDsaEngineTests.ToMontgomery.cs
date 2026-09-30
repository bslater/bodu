// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.ToMontgomery.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that converting to Montgomery form multiplies a coefficient by 2^32 modulo q, within (−q, q).
    /// </summary>
    [TestMethod]
    public void ToMontgomery_WhenCoefficientIsInRange_ShouldMultiplyByTwoToThe32()
    {
        long factor = LatticeCommon.PowMod(2, 32, Q);
        var random = new Random(0x0204_0003);
        IEnumerable<int> values = new[] { 0, 1, Q - 1, -(Q - 1) }.Concat(Enumerable.Range(0, 100_000).Select(_ => random.Next(-Q + 1, Q)));

        foreach (int value in values)
        {
            int converted = MLDsaEngine.ToMontgomery(value);

            if (converted <= -Q || converted >= Q || Mod(converted) != Mod(value * factor))
                Assert.Fail($"ToMontgomery({value}) = {converted}.");
        }
    }

    /// <summary>
    /// Verifies that converting a polynomial to Montgomery form in place converts each coefficient as the scalar
    /// conversion does.
    /// </summary>
    [TestMethod]
    public void ToMontgomery_WhenGivenAPolynomial_ShouldConvertEveryCoefficient()
    {
        foreach (int[] polynomial in Polynomials().Take(20))
        {
            int[] converted = (int[])polynomial.Clone();

            MLDsaEngine.ToMontgomery(converted.AsSpan());

            CollectionAssert.AreEqual(polynomial.Select(MLDsaEngine.ToMontgomery).ToArray(), converted);
        }
    }

    /// <summary>
    /// Verifies that each kernel converts every coefficient of edge, patterned, seeded and signed polynomials to the
    /// Montgomery form the coefficient conversion computes, representative for representative.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void ToMontgomery_WhenGivenAPolynomial_ForEachKernel_ShouldConvertEveryCoefficient(string kernel)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);

        foreach (int[] polynomial in Polynomials().Concat(SignedPolynomials()))
        {
            int[] converted = (int[])polynomial.Clone();

            MLDsaEngine.ToMontgomery(kind, converted);

            CollectionAssert.AreEqual(polynomial.Select(MLDsaEngine.ToMontgomery).ToArray(), converted, $"{kernel}, from {polynomial[0]}");
        }
    }

    /// <summary>
    /// Verifies that converting a polynomial to Montgomery form rejects a span shorter than a polynomial with
    /// <see cref="ArgumentOutOfRangeException" /> naming it, rather than writing past its end, whichever kernel is
    /// named.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Auto")]
    [DataRow("Scalar")]
    [DataRow("Avx2")]
    public void ToMontgomery_WhenSpanIsShorterThanAPolynomial_ShouldThrowArgumentOutOfRangeException(string kernel)
    {
        MLDsaEngine.KernelKind kind = ParseSupportedKernel(kernel);

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            MLDsaEngine.ToMontgomery(kind, new int[MLDsaEngine.N - 1].AsSpan());
        });

        Assert.AreEqual("poly", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the vector kernel's conversion forbids inlining and is aggressively optimized.
    /// </summary>
    [TestMethod]
    public void ToMontgomery_WhenDeclared_ForTheVectorKernel_ShouldForbidInliningIntoTheDispatcher()
    {
        AssertKernelIsCompiledOnItsOwn(nameof(MLDsaEngine.Vector256Kernel.ToMontgomery));
    }
}
