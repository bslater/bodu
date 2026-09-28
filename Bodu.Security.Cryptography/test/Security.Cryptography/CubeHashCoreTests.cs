// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CubeHashCoreTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="CubeHashCore" />, the CubeHash round function behind <see cref="CubeHash" />, grouped into
/// member-named partial files. Every vector kernel, driven explicitly whichever one dispatch picks, is held to the
/// scalar kernel, and the published digests pin the kernel dispatch selects through <see cref="CubeHashTests" />.
/// </summary>
[TestClass]
public sealed partial class CubeHashCoreTests
{
    /// <summary>
    /// Parses a kernel's name, and marks the test inconclusive when the processor cannot run that kernel.
    /// </summary>
    /// <param name="name">The kernel's name.</param>
    /// <returns>The kernel.</returns>
    private static CubeHashCore.KernelKind ParseSupportedKernel(string name)
    {
        CubeHashCore.KernelKind kernel = Enum.Parse<CubeHashCore.KernelKind>(name);
        if (!CubeHashCore.IsSupported(kernel))
            Assert.Inconclusive($"The {name} kernel cannot run on this processor.");

        return kernel;
    }

    /// <summary>
    /// Returns 32 seeded random state words; nothing in the kernels depends on the state holding a CubeHash
    /// initialization vector, so random words exercise every lane of every word.
    /// </summary>
    /// <param name="random">The source of the words.</param>
    /// <returns>The state.</returns>
    private static uint[] NextState(Random random)
    {
        uint[] state = new uint[CubeHashCore.StateWords];
        for (int i = 0; i < state.Length; i++)
            state[i] = (uint)random.NextInt64(0, 1L << 32);

        return state;
    }
}
