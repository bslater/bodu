// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptCoreTests.KernelOperations.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class ScryptCoreTests
{
    /// <summary>
    /// Runs the operations of one BlockMix kernel on blocks given, and returned, in RFC 7914's word order, whatever
    /// order the kernel keeps them in.
    /// </summary>
    /// <param name="Salsa20_8">Applies the Salsa20/8 core in place to one 16-word block.</param>
    /// <param name="BlockMix">Applies BlockMix to an input unit, writing an output unit, for a block size <c>r</c>.</param>
    /// <param name="BlockMixXor">
    /// Applies BlockMix to the XOR of two units, writing an output unit, for a block size <c>r</c>.
    /// </param>
    private sealed record KernelOperations(
        Action<uint[]> Salsa20_8,
        Action<uint[], uint[], int> BlockMix,
        Action<uint[], uint[], uint[], int> BlockMixXor);
}
