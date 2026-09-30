// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2CoreTests.FillBlock.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Argon2CoreTests
{
    /// <summary>The number of seeded random blocks each differential test compresses.</summary>
    private const int DifferentialBlocks = 200;

    /// <summary>
    /// Verifies that each other kernel overwriting its destination - every pass of version 0x10 and the first pass of
    /// version 0x13 - produces the scalar kernel's block and carried state for seeded random inputs.
    /// </summary>
    /// <param name="kernel">The name of the kernel.</param>
    [TestMethod]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("ScalarResident")]
    [DataRow("AdvSimdHybrid")]
    public void FillBlock_WhenOverwriting_ShouldMatchTheScalarKernel(string kernel) =>
        AssertKernelMatchesScalar(ParseSupportedKernel(kernel), withXor: false, referenceIsDestination: false);

    /// <summary>
    /// Verifies that each other kernel XORing into its destination - every pass after the first in version 0x13 -
    /// produces the scalar kernel's block and carried state for seeded random inputs.
    /// </summary>
    /// <param name="kernel">The name of the kernel.</param>
    [TestMethod]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("ScalarResident")]
    [DataRow("AdvSimdHybrid")]
    public void FillBlock_WhenXoring_ShouldMatchTheScalarKernel(string kernel) =>
        AssertKernelMatchesScalar(ParseSupportedKernel(kernel), withXor: true, referenceIsDestination: false);

    /// <summary>
    /// Verifies that each other kernel whose reference block is also its destination - as the Argon2i address
    /// generator compresses its address block in place - reads the reference before overwriting it, as the scalar
    /// kernel does.
    /// </summary>
    /// <param name="kernel">The name of the kernel.</param>
    [TestMethod]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("ScalarResident")]
    [DataRow("AdvSimdHybrid")]
    public void FillBlock_WhenReferenceIsTheDestination_ShouldMatchTheScalarKernel(string kernel) =>
        AssertKernelMatchesScalar(ParseSupportedKernel(kernel), withXor: false, referenceIsDestination: true);

    /// <summary>
    /// Parses a kernel name, reporting the test inconclusive when the processor cannot run that kernel.
    /// </summary>
    /// <param name="name">The kernel's <see cref="Argon2Core.KernelKind" /> name.</param>
    /// <returns>The kernel.</returns>
    private static Argon2Core.KernelKind ParseSupportedKernel(string name)
    {
        Argon2Core.KernelKind kernel = Enum.Parse<Argon2Core.KernelKind>(name);
        if (!Argon2Core.IsSupported(kernel))
            Assert.Inconclusive($"The {name} kernel cannot run on this processor.");

        return kernel;
    }

    /// <summary>
    /// Compresses seeded random blocks with a kernel and with the scalar kernel, and asserts that both leave the same
    /// destination block and the same carried state.
    /// </summary>
    /// <param name="kernel">The kernel under test.</param>
    /// <param name="withXor">Whether the compression XORs into the destination.</param>
    /// <param name="referenceIsDestination">Whether the reference block and the destination are the same block.</param>
    private static void AssertKernelMatchesScalar(Argon2Core.KernelKind kernel, bool withXor, bool referenceIsDestination)
    {
        const int Words = Argon2Matrix.WordsPerBlock;
        var random = new Random(0x4172_6732 + (int)kernel + (withXor ? 1 << 8 : 0) + (referenceIsDestination ? 1 << 9 : 0));
        ulong[] state = new ulong[Words];
        ulong[] reference = new ulong[Words];
        ulong[] next = new ulong[Words];

        for (int block = 0; block < DifferentialBlocks; block++)
        {
            FillRandom(random, state);
            FillRandom(random, reference);
            FillRandom(random, next);
            ulong[] expectedState = (ulong[])state.Clone();
            ulong[] expectedNext = referenceIsDestination ? (ulong[])reference.Clone() : (ulong[])next.Clone();
            ulong[] expectedReference = referenceIsDestination ? expectedNext : (ulong[])reference.Clone();
            ulong[] actualNext = referenceIsDestination ? reference : next;

            FillBlock(Argon2Core.KernelKind.Scalar, expectedState, expectedReference, expectedNext, withXor);
            FillBlock(kernel, state, reference, actualNext, withXor);

            CollectionAssert.AreEqual(expectedNext, actualNext, $"The destination differs at block {block}.");
            CollectionAssert.AreEqual(expectedState, state, $"The carried state differs at block {block}.");
        }
    }

    /// <summary>
    /// Compresses one block with the named kernel, giving it fresh scratch.
    /// </summary>
    /// <param name="kernel">The kernel.</param>
    /// <param name="state">The carried state.</param>
    /// <param name="reference">The reference block.</param>
    /// <param name="next">The destination block, which may be <paramref name="reference" />.</param>
    /// <param name="withXor">Whether the compression XORs into the destination.</param>
    private static void FillBlock(Argon2Core.KernelKind kernel, ulong[] state, ulong[] reference, ulong[] next, bool withXor)
    {
        ulong[] scratch = new ulong[Argon2Matrix.WordsPerBlock];
        switch (kernel)
        {
            case Argon2Core.KernelKind.Avx2:
                FillBlock<Argon2Core.Avx2Kernel>(state, scratch, reference, next, withXor);
                break;

            case Argon2Core.KernelKind.AdvSimd:
                FillBlock<Argon2Core.Vector128Kernel<Argon2Core.AdvSimdIsa>>(state, scratch, reference, next, withXor);
                break;

            case Argon2Core.KernelKind.Ssse3:
                FillBlock<Argon2Core.Vector128Kernel<Argon2Core.Ssse3Isa>>(state, scratch, reference, next, withXor);
                break;

            case Argon2Core.KernelKind.ScalarResident:
                FillBlock<Argon2Core.ResidentScalarKernel>(state, scratch, reference, next, withXor);
                break;

            case Argon2Core.KernelKind.AdvSimdHybrid:
                FillBlock<Argon2Core.HybridKernel<Argon2Core.AdvSimdIsa>>(state, scratch, reference, next, withXor);
                break;

            default:
                FillBlock<Argon2Core.ScalarKernel>(state, scratch, reference, next, withXor);
                break;
        }
    }

    /// <summary>
    /// Compresses one block with the kernel <typeparamref name="TKernel" />.
    /// </summary>
    /// <typeparam name="TKernel">The kernel.</typeparam>
    /// <param name="state">The carried state.</param>
    /// <param name="scratch">The working space.</param>
    /// <param name="reference">The reference block.</param>
    /// <param name="next">The destination block, which may be <paramref name="reference" />.</param>
    /// <param name="withXor">Whether the compression XORs into the destination.</param>
    private static void FillBlock<TKernel>(ulong[] state, ulong[] scratch, ulong[] reference, ulong[] next, bool withXor)
        where TKernel : struct, Argon2Core.IArgon2Kernel =>
        TKernel.FillBlock(ref state[0], ref scratch[0], ref reference[0], ref next[0], withXor);

    /// <summary>
    /// Fills words with seeded random values.
    /// </summary>
    /// <param name="random">The seeded source.</param>
    /// <param name="words">The words to fill.</param>
    private static void FillRandom(Random random, ulong[] words)
    {
        for (int i = 0; i < words.Length; i++)
            words[i] = (ulong)random.NextInt64() ^ ((ulong)random.Next() << 63);
    }
}
