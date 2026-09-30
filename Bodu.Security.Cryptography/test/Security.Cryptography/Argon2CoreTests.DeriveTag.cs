// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2CoreTests.DeriveTag.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

public sealed partial class Argon2CoreTests
{
    /// <summary>The largest memory, in KiB, of the recorded vectors the poisoned-matrix test replays.</summary>
    private const int PoisonedVectorMaxMemoryKiB = 4096;

    /// <summary>
    /// The largest memory, in KiB, of the vectors the kernel and thread sweeps replay. Larger vectors exercise no path
    /// a 16 MiB one does not, and each kernel meets them anyway: AVX2 through the public vector tests on x64, AdvSimd
    /// through the same tests on ARM64, and the scalar kernel through the SIMD opt-out assembly.
    /// </summary>
    private const int SweepMaxMemoryKiB = 16 * 1024;

    /// <summary>
    /// The bounds the thread sweep applies: the calling thread alone, two to four threads, the processor count, and
    /// more threads than any vector has lanes.
    /// </summary>
    private static readonly int[] ThreadBounds = [1, 2, 3, 4, -1, 64];

    /// <summary>
    /// Gets the RFC 9106 vectors and the recorded 1.0.0 vectors of up to 4 MiB: every variant, both versions, and every
    /// lane count and segment shape the corpus covers.
    /// </summary>
    /// <returns>One row per vector, each holding a single <see cref="KdfKnownAnswer" />.</returns>
    public static IEnumerable<object[]> PoisonableVectors() =>
        Argon2Tests.Rfc9106Vectors().Concat(
            Argon2Tests.RecordedCorpusVectors().Where(row => ((KdfKnownAnswer)row[0]).Memory <= PoisonedVectorMaxMemoryKiB));

    /// <summary>
    /// Gets every vector of up to 16 MiB whose lanes can be divided among threads - RFC 9106's, the reference
    /// implementation's with more than one lane, and the recorded 1.0.0 corpus at every lane count from one to eight.
    /// </summary>
    /// <returns>One row per vector, each holding a single <see cref="KdfKnownAnswer" />.</returns>
    public static IEnumerable<object[]> ThreadSweepVectors() =>
        Argon2Tests.Rfc9106Vectors()
            .Concat(Argon2Tests.ReferenceImplementationVectors().Where(row => ((KdfKnownAnswer)row[0]).Parallelism > 1))
            .Concat(Argon2Tests.RecordedCorpusVectors())
            .Where(row => ((KdfKnownAnswer)row[0]).Memory <= SweepMaxMemoryKiB);

    /// <summary>
    /// Gets every vector of up to 16 MiB: RFC 9106's, the reference implementation's, and the recorded 1.0.0 corpus.
    /// </summary>
    /// <returns>One row per vector, each holding a single <see cref="KdfKnownAnswer" />.</returns>
    public static IEnumerable<object[]> KernelSweepVectors() =>
        Argon2Tests.Rfc9106Vectors()
            .Concat(Argon2Tests.ReferenceImplementationVectors())
            .Concat(Argon2Tests.RecordedCorpusVectors())
            .Where(row => ((KdfKnownAnswer)row[0]).Memory <= SweepMaxMemoryKiB);

    /// <summary>
    /// Verifies that every compression kernel the processor supports produces the known tag, whichever one dispatch
    /// would select - so an AVX2 host holds the AVX2, SSSE3, and scalar kernels, and an ARM64 host the AdvSimd and
    /// scalar kernels, to every vector of up to 16 MiB.
    /// </summary>
    /// <param name="vector">The vector derived with each kernel.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(KernelSweepVectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void DeriveTag_WhenEachSupportedKernelFillsTheMatrix_ShouldMatchKnownTag(KdfKnownAnswer vector)
    {
        foreach (Argon2Core.KernelKind kernel in Enum.GetValues<Argon2Core.KernelKind>())
        {
            if (kernel == Argon2Core.KernelKind.Auto || !Argon2Core.IsSupported(kernel))
                continue;

            Assert.AreEqual(vector.ExpectedHex, DeriveHex(vector, new Argon2Core.FillOptions(-1, kernel: kernel)), kernel.ToString());
        }
    }

    /// <summary>
    /// Verifies that a derivation over a matrix pre-filled with garbage still produces the known tag, on the calling
    /// thread alone and with its lanes divided among threads, proving that no block is read before it is written - the
    /// invariant that lets a matrix come uninitialized from native memory or all-zero from the pool.
    /// </summary>
    /// <param name="vector">The vector replayed over the poisoned matrix.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(PoisonableVectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void DeriveTag_WhenMatrixStartsWithGarbage_ShouldMatchKnownTag(KdfKnownAnswer vector)
    {
        var pool = new NativeBufferPool(0, 0, TimeSpan.FromHours(1), TimeProvider.System);

        foreach (int bound in new[] { 1, 4 })
        {
            using Argon2Matrix matrix = Argon2Matrix.Rent(MemoryBlocks(vector), pool);
            for (int block = 0; block < matrix.BlockCount; block++)
                matrix.BlockSpan(block).Fill(0xDEAD_BEEF_A5A5_5A5AUL ^ ((ulong)block * 0x9E37_79B9_7F4A_7C15UL));

            byte[] tag = new byte[vector.OutputLength];
            Argon2Core.DeriveTag(ToType(vector), ToParameters(vector), vector.Password, vector.Salt, tag, matrix, OnThreads(bound));

            Assert.AreEqual(vector.ExpectedHex, Convert.ToHexString(tag).ToLowerInvariant(), $"bound {bound}");
        }
    }

    /// <summary>
    /// Verifies that a derivation whose every slice is divided among threads produces the known tag at every bound,
    /// whatever the lane count and segment shape.
    /// </summary>
    /// <param name="vector">The vector derived at each bound.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(ThreadSweepVectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void DeriveTag_WhenLanesAreDividedAmongThreads_ShouldMatchKnownTag(KdfKnownAnswer vector)
    {
        foreach (int bound in ThreadBounds)
            Assert.AreEqual(vector.ExpectedHex, DeriveHex(vector, OnThreads(bound)), $"bound {bound}");
    }

    /// <summary>
    /// Verifies that many derivations running at once, each dividing its lanes among threads and all sharing the
    /// matrix pool, each produce their known tag.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void DeriveTag_WhenManyDerivationsRunAtOnce_ShouldEachMatchKnownTag()
    {
        const int Repetitions = 3;
        KdfKnownAnswer[] vectors =
        [
            .. Argon2Tests.RecordedCorpusVectors()
                .Select(row => (KdfKnownAnswer)row[0])
                .Where(vector => vector.Parallelism > 1 && vector.Memory <= PoisonedVectorMaxMemoryKiB),
        ];
        var failures = new ConcurrentQueue<string>();

        Parallel.For(0, vectors.Length * Repetitions, new ParallelOptions { MaxDegreeOfParallelism = 2 * Environment.ProcessorCount }, index =>
        {
            KdfKnownAnswer vector = vectors[index % vectors.Length];
            if (DeriveHex(vector, OnThreads(-1)) != vector.ExpectedHex)
                failures.Enqueue(vector.Name);
        });

        Assert.IsEmpty(failures, string.Join("; ", failures));
    }

    /// <summary>
    /// Verifies that a fault in a segment filled on a thread surfaces as itself, not wrapped in an
    /// <see cref="AggregateException" />, as it would on the calling thread alone.
    /// </summary>
    /// <remarks>
    /// The matrix holds the first two columns of every lane but stops short of the rest, so the last lane's first
    /// segment writes past its end and the bounds check faults inside the fork.
    /// </remarks>
    [TestMethod]
    public void DeriveTag_WhenASegmentFaultsOnAThread_ShouldSurfaceTheFaultItself()
    {
        const int LaneLength = 256;
        var parameters = new Argon2Parameters { MemoryKiB = 4 * LaneLength, Iterations = 1, Parallelism = 4 };
        byte[] password = new byte[16];
        byte[] salt = new byte[16];
        byte[] tag = new byte[parameters.TagLength];
        var pool = new NativeBufferPool(0, 0, TimeSpan.FromHours(1), TimeProvider.System);
        using Argon2Matrix matrix = Argon2Matrix.Rent((3 * LaneLength) + 2, pool);

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Argon2Core.DeriveTag(Argon2Type.Argon2id, parameters, password, salt, tag, matrix, OnThreads(4));
        });

        Assert.AreEqual("index", ex.ParamName);
    }
}
