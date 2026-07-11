// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageBuilderSerializationTests.Async.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.Builders;
using Bodu.IO.Compound.Internal;
using Bodu.Test.IO;
using Bodu.Test.Kat;

namespace Bodu.IO.Compound;

public partial class CompoundStorageBuilderSerializationTests
{
    /// <summary>
    /// Verifies that the asynchronous serialize path reproduces the committed golden file byte-for-byte, so it can
    /// never drift from the synchronous emitter.
    /// </summary>
    /// <param name="kat">The golden row.</param>
    [TestMethod]
    [DynamicData(nameof(GoldenRows), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public async Task WriteToAsync_WhenCanonicalTree_ShouldMatchGoldenFileByteForByte(CompoundStorageBuilderGoldenKat kat)
    {
        byte[] expected = CompoundFixtures.ReadWriterGolden(kat.FileName);

        using var actual = new MemoryStream();
        await CompoundContainerLayout.WriteToAsync(actual, BuildCanonical(), new CompoundBuildOptions { Version = kat.Version }, CancellationToken.None);

        CollectionAssert.AreEqual(expected, actual.ToArray());
    }

    /// <summary>
    /// Verifies that the asynchronous serialize path over a tree mixing inline and deferred stream sources produces
    /// exactly the same bytes as the synchronous path.
    /// </summary>
    [TestMethod]
    public async Task WriteToAsync_WhenMixedTree_ShouldMatchSyncPathByteForByte()
    {
        static CompoundStorageBuilder BuildMixed()
        {
            var builder = CompoundStorageBuilder.CreateRoot();
            _ = builder.AddStream("Inline", Canonical(200));
            _ = builder.AddStream("Deferred", () => new MemoryStream(Canonical(9000)), 9000);
            CompoundStorageBuilder nested = builder.AddStorage("Nested");
            _ = nested.AddStream("Mini", Canonical(48));
            return builder;
        }

        var options = new CompoundBuildOptions();

        using var sync = new MemoryStream();
        CompoundContainerLayout.WriteTo(sync, BuildMixed(), options);

        using var async = new MemoryStream();
        await CompoundContainerLayout.WriteToAsync(async, BuildMixed(), options, CancellationToken.None);

        CollectionAssert.AreEqual(sync.ToArray(), async.ToArray());
    }

    /// <summary>
    /// Verifies that cancellation observed while copying a deferred stream source surfaces as
    /// <see cref="OperationCanceledException" /> during an asynchronous write.
    /// </summary>
    [TestMethod]
    public async Task WriteToAsync_WhenCancelledMidDeferredCopy_ShouldThrowOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();

        const int payloadSize = 300_000;
        var root = CompoundStorageBuilder.CreateRoot();
        _ = root.AddStream("Big", () => new CancellationTriggerStream(new IncrementingByteStream(payloadSize), cts, cancelAfterRead: 2), payloadSize);

        using var destination = new MemoryStream();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await CompoundContainerLayout.WriteToAsync(destination, root, new CompoundBuildOptions(), cts.Token));
    }

    /// <summary>
    /// Verifies that a deferred source shorter than its declared length throws
    /// <see cref="CompoundFileSerializationException" /> during an asynchronous write.
    /// </summary>
    [TestMethod]
    public async Task WriteToAsync_WhenDeferredSourceShort_ShouldThrowCompoundFileSerializationException()
    {
        var root = CompoundStorageBuilder.CreateRoot();
        _ = root.AddStream("Big", () => new MemoryStream(Canonical(4096)), 8192);

        using var destination = new MemoryStream();

        await Assert.ThrowsExactlyAsync<CompoundFileSerializationException>(async () =>
            await CompoundContainerLayout.WriteToAsync(destination, root, new CompoundBuildOptions(), CancellationToken.None));
    }
}
