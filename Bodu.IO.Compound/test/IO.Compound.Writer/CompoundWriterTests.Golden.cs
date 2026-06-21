// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundWriterTests.Golden.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.Nodes;
using Bodu.Test;
using Bodu.Test.Kat;

namespace Bodu.IO.Compound.Writer;

public partial class CompoundWriterTests
{
    /// <summary>
    /// Gets the golden byte-for-byte rows pairing a version with its committed reference file.
    /// </summary>
    /// <returns>A sequence of single-element argument arrays wrapping the rows.</returns>
    public static IEnumerable<object[]> GoldenRows()
    {
        yield return [new CompoundWriterGoldenKat(CompoundFileVersion.V3, "golden-v3.cfb")];
        yield return [new CompoundWriterGoldenKat(CompoundFileVersion.V4, "golden-v4.cfb")];
    }

    /// <summary>
    /// Verifies that serializing the canonical tree reproduces the committed golden file byte-for-byte, guarding the
    /// exact on-disk layout against regressions.
    /// </summary>
    /// <param name="kat">The golden row.</param>
    [TestMethod]
    [DynamicData(nameof(GoldenRows), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ToArray_WhenCanonicalTree_ShouldMatchGoldenFileByteForByte(CompoundWriterGoldenKat kat)
    {
        byte[] expected = CompoundFixtures.ReadWriterGolden(kat.FileName);

        byte[] actual = BuildCanonical().ToArray(new CompoundWriterOptions { Version = kat.Version });

        Assert.AreEqual(expected.Length, actual.Length, "length");
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that writing is a fixed point: a golden file, loaded and re-serialized, is byte-identical to itself.
    /// </summary>
    /// <param name="kat">The golden row.</param>
    [TestMethod]
    [DynamicData(nameof(GoldenRows), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Save_WhenReloadingGolden_ShouldBeByteStable(CompoundWriterGoldenKat kat)
    {
        byte[] golden = CompoundFixtures.ReadWriterGolden(kat.FileName);

        byte[] rewritten = CompoundStorageNode.Load(new MemoryStream(golden))
            .ToArray(new CompoundWriterOptions { Version = kat.Version });

        CollectionAssert.AreEqual(golden, rewritten);
    }

    /// <summary>
    /// Builds the canonical, fully-deterministic tree used to generate the golden files.
    /// </summary>
    /// <returns>The canonical root storage.</returns>
    private static CompoundStorageNode BuildCanonical()
    {
        CompoundStorageNode root = CompoundStorageNode.CreateRoot();
        root.ClassId = new Guid("00020906-0000-0000-C000-000000000046");
        CompoundStorageNode data = root.AddStorage("Data");
        _ = data.AddStream("Small", Canonical(100));
        _ = data.AddStream("Large", Canonical(5000));
        _ = root.AddStream("Meta", Canonical(48));
        return root;
    }

    /// <summary>
    /// Generates the deterministic payload used by the canonical tree.
    /// </summary>
    /// <param name="size">The payload length.</param>
    /// <returns>The payload.</returns>
    private static byte[] Canonical(int size)
    {
        byte[] payload = new byte[size];
        for (int i = 0; i < size; i++)
            payload[i] = (byte)((i * 31) + 7);

        return payload;
    }
}

/// <summary>
/// A known-answer row pairing a writer version with its committed golden file name.
/// </summary>
/// <param name="Version">The compound-file version.</param>
/// <param name="FileName">The golden fixture file name.</param>
public sealed record CompoundWriterGoldenKat(CompoundFileVersion Version, string FileName)
    : IKat
{
    /// <inheritdoc />
    public string Name => FileName;
}
