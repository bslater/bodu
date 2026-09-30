// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Tests.Allocation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Argon2Tests
{
    /// <summary>The managed allocation a derivation may make, in bytes: FallbackPlan's requirement ARG-N-004.</summary>
    private const long AllocationBudgetBytes = 1024 * 1024;

    /// <summary>
    /// Verifies that a steady loop of derivations allocates well under 1 MiB of managed memory per call, even with a
    /// 4 MiB matrix - the matrix lives in native memory, not on the collected heap.
    /// </summary>
    /// <remarks>
    /// Measured on the calling thread with a single lane, so allocations made by other tests running in parallel do not
    /// count; process-wide collection counts are left to the benchmarks for the same reason.
    /// </remarks>
    [TestMethod]
    public void DeriveKey_WhenDerivingRepeatedly_ShouldAllocateLessThanOneMebibytePerCall()
    {
        const int Calls = 4;
        var parameters = new Argon2Parameters { MemoryKiB = 4096, Iterations = 1, Parallelism = 1 };
        byte[] password = Repeat(0x11, 16);
        byte[] salt = Repeat(0x22, 16);
        _ = Argon2id.DeriveKey(password, salt, parameters);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int call = 0; call < Calls; call++)
            _ = Argon2id.DeriveKey(password, salt, parameters);

        long perCall = (GC.GetAllocatedBytesForCurrentThread() - before) / Calls;

        Assert.IsLessThan(AllocationBudgetBytes, perCall, $"Allocated {perCall} bytes per derivation.");
    }
}
