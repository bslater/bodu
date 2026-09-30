// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NativeBufferPoolTests.Rent.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class NativeBufferPoolTests
{
    /// <summary>
    /// Verifies that renting from an empty pool yields a matrix of the requested blocks whose every block can be written.
    /// </summary>
    [TestMethod]
    public void Rent_WhenNothingIsRetained_ShouldAllocateTheRequestedBlocks()
    {
        NativeBufferPool pool = CreatePool();

        using Argon2Matrix matrix = Argon2Matrix.Rent(16, pool);
        FillWithPattern(matrix);

        Assert.AreEqual(16, matrix.BlockCount);
        Assert.AreEqual(0, pool.RetainedCount);
    }

    /// <summary>
    /// Verifies that renting after a buffer has been returned takes the retained buffer instead of allocating.
    /// </summary>
    [TestMethod]
    public void Rent_WhenABufferIsRetained_ShouldReuseIt()
    {
        NativeBufferPool pool = CreatePool();
        Argon2Matrix.Rent(16, pool).Dispose();

        using Argon2Matrix matrix = Argon2Matrix.Rent(16, pool);

        Assert.AreEqual(0, pool.RetainedCount);
    }

    /// <summary>
    /// Verifies that renting takes the smallest retained buffer that is large enough, leaving larger ones for larger
    /// derivations.
    /// </summary>
    [TestMethod]
    public void Rent_WhenSeveralBuffersFit_ShouldTakeTheSmallest()
    {
        NativeBufferPool pool = CreatePool();
        Argon2Matrix large = Argon2Matrix.Rent(64, pool);
        Argon2Matrix small = Argon2Matrix.Rent(16, pool);
        large.Dispose();
        small.Dispose();

        using Argon2Matrix matrix = Argon2Matrix.Rent(8, pool);

        Assert.AreEqual(64L * BlockBytes, pool.RetainedBytes);
    }

    /// <summary>
    /// Verifies that the shared pool retains up to one buffer per processor, each of up to 256 MiB, for thirty idle
    /// seconds - the defaults the documentation states.
    /// </summary>
    [TestMethod]
    public void Shared_WhenCreated_ShouldUseTheDocumentedDefaults()
    {
        NativeBufferPool shared = NativeBufferPool.Shared;

        Assert.AreEqual(Environment.ProcessorCount, shared.MaxRetainedBuffers);
        Assert.AreEqual(256L * 1024 * 1024, shared.MaxRetainedBufferBytes);
        Assert.AreEqual(TimeSpan.FromSeconds(30), shared.IdleTimeout);
    }
}
