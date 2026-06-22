// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageBuilderSerializationTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.IO.Compound.Builders;
using Bodu.Test;
using Bodu.Test.Kat;

namespace Bodu.IO.Compound;

/// <summary>
/// A known-answer row describing a single-stream round-trip at a given size and version, covering the mini-stream and
/// regular-FAT boundaries and multi-sector payloads.
/// </summary>
/// <param name="Version">The compound-file version to write.</param>
/// <param name="Size">The stream size, in bytes.</param>
public sealed record CompoundStorageBuilderSizeKat(CompoundFileVersion Version, int Size)
    : IKat
{
    /// <inheritdoc />
    public string Name => $"{Version} size={Size}";
}

public partial class CompoundStorageBuilderSerializationTests
{
    /// <summary>
    /// Gets the size/version round-trip rows spanning the mini/regular boundary and multi-sector payloads.
    /// </summary>
    /// <returns>A sequence of single-element argument arrays wrapping the rows.</returns>
    public static IEnumerable<object[]> SizeRows()
    {
        int[] sizes = [0, 1, 63, 64, 4095, 4096, 4097, 8192, 20000];
        foreach (CompoundFileVersion version in new[] { CompoundFileVersion.V3, CompoundFileVersion.V4 })
        {
            foreach (int size in sizes)
                yield return [new CompoundStorageBuilderSizeKat(version, size)];
        }
    }

    /// <summary>
    /// Verifies that a single stream of a given size round-trips byte-for-byte across the mini/regular boundary and for
    /// both versions.
    /// </summary>
    /// <param name="kat">The size/version row.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(SizeRows), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ToArray_WhenStreamOfSize_ShouldRoundTripExactly(CompoundStorageBuilderSizeKat kat)
    {
        byte[] payload = CreatePayload(kat.Size);
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("Data", payload);

        byte[] bytes = builder.ToArray(new CompoundBuildOptions { Version = kat.Version });

        using CompoundFile file = CompoundFile.Open(new MemoryStream(bytes));
        Assert.IsTrue(file.RootStorage.TryOpenStream("Data", out CompoundStream? entry));
        Assert.AreEqual(kat.Size, entry.Length);
        CollectionAssert.AreEqual(payload, entry.ReadAllBytes());
    }

    /// <summary>
    /// Verifies that a storage with many sibling streams round-trips, exercising the red-black directory tree.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void ToArray_WhenManySiblings_ShouldRoundTripAllStreams()
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        for (int i = 0; i < 100; i++)
            _ = builder.AddStream($"Stream{i:D3}", CreatePayload(i));

        using CompoundFile file = CompoundFile.Open(new MemoryStream(builder.ToArray()));

        for (int i = 0; i < 100; i++)
        {
            Assert.IsTrue(file.RootStorage.TryOpenStream($"Stream{i:D3}", out CompoundStream? entry), $"Stream{i:D3}");
            CollectionAssert.AreEqual(CreatePayload(i), entry.ReadAllBytes(), $"Stream{i:D3}");
        }
    }

    /// <summary>
    /// Verifies that a deeply nested storage hierarchy round-trips.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void ToArray_WhenDeeplyNested_ShouldRoundTrip()
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        CompoundStorageBuilder current = builder;
        for (int depth = 0; depth < 8; depth++)
            current = current.AddStorage($"Level{depth}");

        _ = current.AddStream("Leaf", CreatePayload(200));

        using CompoundFile file = CompoundFile.Open(new MemoryStream(builder.ToArray()));
        CompoundStorage storage = file.RootStorage;
        for (int depth = 0; depth < 8; depth++)
            storage = storage.OpenStorage($"Level{depth}");

        Assert.IsTrue(storage.TryOpenStream("Leaf", out CompoundStream? leaf));
        CollectionAssert.AreEqual(CreatePayload(200), leaf.ReadAllBytes());
    }

    /// <summary>
    /// Creates a deterministic payload of the requested length.
    /// </summary>
    /// <param name="size">The payload length.</param>
    /// <returns>The generated payload.</returns>
    private static byte[] CreatePayload(int size)
    {
        byte[] payload = new byte[size];
        for (int i = 0; i < size; i++)
            payload[i] = (byte)((i * 31) + 7);

        return payload;
    }

    /// <summary>
    /// Computes the lowercase hexadecimal SHA-256 of a payload.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <returns>The hash string.</returns>
    private static string Hash(ReadOnlySpan<byte> data) =>
        Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
}
