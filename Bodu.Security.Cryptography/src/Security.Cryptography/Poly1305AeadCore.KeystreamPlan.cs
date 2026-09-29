// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305AeadCore.KeystreamPlan.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography;

internal static partial class Poly1305AeadCore
{
    /// <summary>The longest run of keystream, in bytes, that a message draws in one pass through a buffer on the stack, the block that keys Poly1305 included: sixteen blocks, one group of the widest kernel.</summary>
    private const int OnePassMaximumBytes = 16 * KeystreamBlockBytes;

    /// <summary>The plans <see cref="OnePassBlocks" /> has built, one for each kernel: for every count of keystream bytes up to <see cref="OnePassMaximumBytes" />, the value <see cref="PlanOnePass" /> returns for it.</summary>
    private static readonly byte[]?[] s_onePassPlans = new byte[]?[(int)ChaCha20Core.KernelKind.Avx512Wide + 1];

    /// <summary>
    /// Returns the number of blocks in the run that draws a message's keystream in one pass through a buffer, or zero
    /// where the message draws the key block and then its own blocks.
    /// </summary>
    /// <param name="kernel">The kernel the keystream runs.</param>
    /// <param name="keystreamBytes">
    /// The number of keystream bytes the message needs, the key block's included: the length of the message plus 64
    /// under RFC 8439, or plus 32 under secretbox.
    /// </param>
    /// <returns>The value <see cref="PlanOnePass" /> returns, looked up in a plan built once for the kernel.</returns>
    /// <remarks>
    /// Planning a message afresh costs about half as much as one of its blocks, so the plan for each count of bytes up
    /// to <see cref="OnePassMaximumBytes" /> is computed on the kernel's first message and kept. Two threads that start
    /// on the same kernel at once may each compute it; the plans are equal, and the first kept serves both.
    /// </remarks>
    internal static int OnePassBlocks(ChaCha20Core.KernelKind kernel, int keystreamBytes)
    {
        if (keystreamBytes > OnePassMaximumBytes)
            return 0;

        byte[] plan = s_onePassPlans[(int)kernel] ?? BuildOnePassPlan(kernel);

        return plan[keystreamBytes];
    }

    /// <summary>
    /// Decides whether a message's keystream is estimated to cost less drawn in one pass through a buffer than drawn as
    /// it comes: the key block, then the message's own blocks.
    /// </summary>
    /// <param name="kernel">The kernel the keystream runs.</param>
    /// <param name="keystreamBytes">The number of keystream bytes the message needs, the key block's included.</param>
    /// <returns>
    /// The number of blocks in the cheapest run that covers <paramref name="keystreamBytes" />, if it costs less than
    /// drawing the keystream as it comes; zero if it costs as much or more, or if the run would be longer than
    /// <see cref="OnePassMaximumBytes" />.
    /// </returns>
    /// <remarks>
    /// The costs are the estimates of <see cref="ChaCha20Core.CostFor" />, the run's against the key block's plus
    /// <see cref="KeystreamCost" />'s for the rest. Where they tie, the keystream is drawn as it comes, which copies
    /// nothing through the buffer. On the block function every block costs the same however it is drawn, so the costs
    /// always tie there, and no message on it takes one pass.
    /// </remarks>
    internal static int PlanOnePass(ChaCha20Core.KernelKind kernel, int keystreamBytes)
    {
        if (keystreamBytes > OnePassMaximumBytes)
            return 0;

        int runBlocks = CheapestRunBlocks(kernel, (keystreamBytes + KeystreamBlockBytes - 1) / KeystreamBlockBytes);
        int separate = ChaCha20Core.StepCost(kernel, 1) + KeystreamCost(kernel, Math.Max(0, keystreamBytes - KeystreamBlockBytes));

        return ChaCha20Core.CostFor(kernel, runBlocks) < separate ? runBlocks : 0;
    }

    /// <summary>
    /// Returns the number of blocks in the cheapest run through a buffer that covers the specified number of blocks:
    /// the blocks themselves, or rounded up to a multiple of four blocks, at most sixteen.
    /// </summary>
    /// <param name="kernel">The kernel the keystream runs.</param>
    /// <param name="blocks">The number of blocks the run must cover, at most sixteen.</param>
    /// <returns>
    /// The number of blocks whose <see cref="ChaCha20Core.CostFor" /> is least; the fewest, where several tie.
    /// </returns>
    /// <remarks>
    /// Rounding up can lower the cost, since a kernel computes its whole group at once: on AVX-512, three blocks cost
    /// three calls of the block function, and a run of four one step.
    /// </remarks>
    internal static int CheapestRunBlocks(ChaCha20Core.KernelKind kernel, int blocks)
    {
        int cheapest = blocks;
        int cheapestCost = ChaCha20Core.CostFor(kernel, blocks);

        for (int candidate = (blocks + 3) / 4 * 4; candidate <= 16; candidate += 4)
        {
            int cost = ChaCha20Core.CostFor(kernel, candidate);
            if (cost < cheapestCost)
            {
                cheapest = candidate;
                cheapestCost = cost;
            }
        }

        return cheapest;
    }

    /// <summary>
    /// Estimates what <see cref="XorKeystream" /> costs over the specified number of bytes, in the units of
    /// <see cref="ChaCha20Core.StepCost" />.
    /// </summary>
    /// <param name="kernel">The kernel the keystream runs.</param>
    /// <param name="bytes">The number of bytes.</param>
    /// <returns>The estimated cost of the keystream drawn as <see cref="XorKeystream" /> draws it.</returns>
    internal static int KeystreamCost(ChaCha20Core.KernelKind kernel, int bytes)
    {
        int rest = bytes % (GroupBlocks(kernel) * KeystreamBlockBytes);
        int runBlocks = RestRunBlocks(kernel, rest);

        if (runBlocks != 0)
            return ChaCha20Core.CostFor(kernel, (bytes - rest) / KeystreamBlockBytes) + ChaCha20Core.CostFor(kernel, runBlocks);

        int partial = bytes % KeystreamBlockBytes == 0 ? 0 : ChaCha20Core.StepCost(kernel, 1);

        return ChaCha20Core.CostFor(kernel, bytes / KeystreamBlockBytes) + partial;
    }

    /// <summary>
    /// Decides whether the bytes after a message's whole groups of the narrowest kernel are estimated to cost less
    /// drawn through a buffer in one step of that kernel than drawn a block at a time.
    /// </summary>
    /// <param name="kernel">The kernel the keystream runs.</param>
    /// <param name="restBytes">The number of bytes after the whole groups: fewer than a group.</param>
    /// <returns>The number of blocks in a group, if the step is estimated to cost less; otherwise, zero.</returns>
    /// <remarks>
    /// <para>
    /// Drawn a block at a time, each of the rest's blocks, the partial last one included, costs one call of the block
    /// function, since a run of fewer blocks than a group leaves the kernels to it.
    /// </para>
    /// <para>
    /// Every message drawn as it comes asks, so the decision is inlined: for the kernel of a keystream known when the
    /// framing is compiled, it folds to one comparison of the rest's length.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int RestRunBlocks(ChaCha20Core.KernelKind kernel, int restBytes)
    {
        int group = GroupBlocks(kernel);
        int blocks = (restBytes + KeystreamBlockBytes - 1) / KeystreamBlockBytes;

        return group > 1 && ChaCha20Core.StepCost(kernel, group) < blocks * ChaCha20Core.StepCost(kernel, 1) ? group : 0;
    }

    /// <summary>
    /// Returns the number of blocks the narrowest kernel of the specified kind computes at once.
    /// </summary>
    /// <param name="kernel">The kernel the keystream runs.</param>
    /// <returns>4 for a vector kernel; 1 for the block function.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GroupBlocks(ChaCha20Core.KernelKind kernel) =>
        ChaCha20Core.LanesFor(kernel, ChaCha20Core.NarrowestKernelLanes);

    /// <summary>
    /// Computes the one-pass plan for a kernel, <see cref="PlanOnePass" /> for every count of keystream bytes up to
    /// <see cref="OnePassMaximumBytes" />, and keeps it unless another thread has kept one first.
    /// </summary>
    /// <param name="kernel">The kernel the keystream runs.</param>
    /// <returns>The plan kept for the kernel, indexed by the count of keystream bytes.</returns>
    /// <remarks>
    /// The plan is published with a full fence, so a thread that reads it sees every entry filled in.
    /// </remarks>
    private static byte[] BuildOnePassPlan(ChaCha20Core.KernelKind kernel)
    {
        byte[] plan = new byte[OnePassMaximumBytes + 1];

        for (int bytes = 0; bytes < plan.Length; bytes++)
            plan[bytes] = (byte)PlanOnePass(kernel, bytes);

        return Interlocked.CompareExchange(ref s_onePassPlans[(int)kernel], plan, null) ?? plan;
    }
}
