// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptCoreTests.IScryptKernel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// The BlockMix kernels: each kernel the processor can run is held to RFC 7914's Salsa20/8 and BlockMix vectors and to
/// its own BlockMix on the XOR it folds in, through its word order.
/// </summary>
public sealed partial class ScryptCoreTests
{
    /// <summary>
    /// Verifies that each kernel's Salsa20/8 core reproduces RFC 7914, Section 8's vector.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Sse2")]
    [DataRow("AdvSimd")]
    public void Salsa20_8_WhenGivenRfc7914Input_ForEachKernel_ShouldMatchRfc7914Vector(string kernel)
    {
        uint[] block = ToWords(SalsaInputHex);

        OperationsOf(kernel).Salsa20_8(block);

        Assert.AreEqual(SalsaOutputHex, ToHex(block));
    }

    /// <summary>
    /// Verifies that each kernel's BlockMix reproduces RFC 7914, Section 9's vector, with <c>r = 1</c>.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Sse2")]
    [DataRow("AdvSimd")]
    public void BlockMix_WhenGivenRfc7914Input_ForEachKernel_ShouldMatchRfc7914Vector(string kernel)
    {
        uint[] output = new uint[32];

        OperationsOf(kernel).BlockMix(ToWords(RomixInputHex), output, 1);

        Assert.AreEqual(BlockMixOutputHex, ToHex(output));
    }

    /// <summary>
    /// Verifies that each kernel's BlockMix matches the scalar kernel's on seeded random units of several block sizes.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Sse2")]
    [DataRow("AdvSimd")]
    public void BlockMix_WhenUnitsAreRandom_ForEachKernel_ShouldMatchScalarKernel(string kernel)
    {
        KernelOperations operations = OperationsOf(kernel);
        KernelOperations scalar = OperationsOf(nameof(ScryptCore.KernelKind.Scalar));

        foreach (int blockSizeR in new[] { 1, 2, 3, 8, 16 })
        {
            for (int seed = 0; seed < 8; seed++)
            {
                uint[] input = RandomWords(32 * blockSizeR, (blockSizeR * 100) + seed);
                uint[] expected = new uint[input.Length];
                uint[] actual = new uint[input.Length];

                scalar.BlockMix(input, expected, blockSizeR);
                operations.BlockMix(input, actual, blockSizeR);

                CollectionAssert.AreEqual(expected, actual, $"r = {blockSizeR}, seed {seed}");
            }
        }
    }

    /// <summary>
    /// Verifies that each kernel's fused BlockMix of <c>X xor V[j]</c> equals its BlockMix of the XOR written out, on
    /// seeded random units of several block sizes.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Sse2")]
    [DataRow("AdvSimd")]
    public void BlockMixXor_WhenUnitsAreRandom_ForEachKernel_ShouldMatchBlockMixOfTheXor(string kernel)
    {
        KernelOperations operations = OperationsOf(kernel);

        foreach (int blockSizeR in new[] { 1, 2, 3, 8 })
        {
            uint[] x = RandomWords(32 * blockSizeR, blockSizeR);
            uint[] v = RandomWords(32 * blockSizeR, blockSizeR + 50);
            uint[] xor = new uint[x.Length];
            for (int i = 0; i < xor.Length; i++)
                xor[i] = x[i] ^ v[i];

            uint[] expected = new uint[x.Length];
            uint[] actual = new uint[x.Length];
            operations.BlockMix(xor, expected, blockSizeR);
            operations.BlockMixXor(x, v, actual, blockSizeR);

            CollectionAssert.AreEqual(expected, actual, $"r = {blockSizeR}");
        }
    }

    /// <summary>
    /// Verifies that each kernel's word order keeps word 0 of every block first, where ROMix reads <c>Integerify</c>,
    /// and that exporting a unit undoes importing it.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Sse2")]
    [DataRow("AdvSimd")]
    public void Import_WhenWordsAreNumbered_ForEachKernel_ShouldKeepWordZeroFirstAndRoundTripThroughExport(string kernel)
    {
        uint[] unit = new uint[32 * 3];
        for (int i = 0; i < unit.Length; i++)
            unit[i] = (uint)i;

        uint[] imported = (uint[])unit.Clone();
        ImportThenExport(ParseSupportedKernel(kernel), imported, out uint[] afterImport);

        for (int block = 0; block < 6; block++)
            Assert.AreEqual(unit[block * 16], afterImport[block * 16], $"block {block}");

        CollectionAssert.AreEqual(unit, imported);
    }

    /// <summary>
    /// Imports a unit of six blocks into a kernel's word order, records it, and exports it back in place.
    /// </summary>
    /// <param name="kernel">The kernel.</param>
    /// <param name="unit">The unit, in RFC 7914's order on entry and on return.</param>
    /// <param name="afterImport">The unit as it stood in the kernel's order.</param>
    private static void ImportThenExport(ScryptCore.KernelKind kernel, uint[] unit, out uint[] afterImport)
    {
        switch (kernel)
        {
            case ScryptCore.KernelKind.Sse2:
                ImportThenExport<ScryptCore.Vector128Kernel<ScryptCore.Sse2Isa>>(unit, out afterImport);
                break;

            case ScryptCore.KernelKind.AdvSimd:
                ImportThenExport<ScryptCore.Vector128Kernel<ScryptCore.AdvSimdIsa>>(unit, out afterImport);
                break;

            default:
                ImportThenExport<ScryptCore.ScalarKernel>(unit, out afterImport);
                break;
        }
    }

    /// <summary>
    /// Imports a unit of six blocks into a kernel's word order, records it, and exports it back in place.
    /// </summary>
    /// <typeparam name="TKernel">The kernel.</typeparam>
    /// <param name="unit">The unit, in RFC 7914's order on entry and on return.</param>
    /// <param name="afterImport">The unit as it stood in the kernel's order.</param>
    private static void ImportThenExport<TKernel>(uint[] unit, out uint[] afterImport)
        where TKernel : struct, ScryptCore.IScryptKernel
    {
        TKernel.Import(ref unit[0], 6);
        afterImport = (uint[])unit.Clone();
        TKernel.Export(ref unit[0], 6);
    }
}
