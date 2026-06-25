// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageBuilderSerializationTests.Golden.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.Builders;
using Bodu.Test;
using Bodu.Test.Kat;

namespace Bodu.IO.Compound;

public partial class CompoundStorageBuilderSerializationTests
{
    /// <summary>
    /// Gets the golden byte-for-byte rows pairing a version with its committed reference file.
    /// </summary>
    /// <returns>A sequence of single-element argument arrays wrapping the rows.</returns>
    public static IEnumerable<object[]> GoldenRows()
    {
        yield return [new CompoundStorageBuilderGoldenKat(CompoundFileVersion.V3, "golden-v3.cfb")];
        yield return [new CompoundStorageBuilderGoldenKat(CompoundFileVersion.V4, "golden-v4.cfb")];
    }

    /// <summary>
    /// Verifies that serializing the canonical tree reproduces the committed golden file byte-for-byte, guarding the
    /// exact on-disk layout against regressions.
    /// </summary>
    /// <param name="kat">The golden row.</param>
    [TestMethod]
    [DynamicData(nameof(GoldenRows), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ToArray_WhenCanonicalTree_ShouldMatchGoldenFileByteForByte(CompoundStorageBuilderGoldenKat kat)
    {
        byte[] expected = CompoundFixtures.ReadWriterGolden(kat.FileName);

        byte[] actual = BuildCanonical().ToArray(new CompoundBuildOptions { Version = kat.Version });

        Assert.HasCount(expected.Length, actual, "length");
        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that writing is a fixed point: a golden file, loaded and re-serialized, is byte-identical to itself.
    /// </summary>
    /// <param name="kat">The golden row.</param>
    [TestMethod]
    [DynamicData(nameof(GoldenRows), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Load_WhenReloadingGolden_ShouldBeByteStable(CompoundStorageBuilderGoldenKat kat)
    {
        byte[] golden = CompoundFixtures.ReadWriterGolden(kat.FileName);

        byte[] rewritten = CompoundStorageBuilder
            .Load(new MemoryStream(golden))
            .ToArray(new CompoundBuildOptions { Version = kat.Version });

        CollectionAssert.AreEqual(golden, rewritten);
    }

    /// <summary>
    /// Builds the canonical, fully-deterministic tree used to generate the golden files.
    /// </summary>
    /// <returns>The populated root storage tree.</returns>
    private static CompoundStorageBuilder BuildCanonical()
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        builder.ClassId = new Guid("00020906-0000-0000-C000-000000000046");
        CompoundStorageBuilder data = builder.AddStorage("Data");
        _ = data.AddStream("Small", Canonical(100));
        _ = data.AddStream("Large", Canonical(5000));
        _ = builder.AddStream("Meta", Canonical(48));
        return builder;
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
/// A known-answer row pairing a builder version with its committed golden file name.
/// </summary>
/// <param name="Version">The compound-file version.</param>
/// <param name="FileName">The golden fixture file name.</param>
public sealed record CompoundStorageBuilderGoldenKat(CompoundFileVersion Version, string FileName)
    : IKat
{
    /// <inheritdoc />
    public string Name => FileName;
}
