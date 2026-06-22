// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileBuilderLoadTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.IO.Compound.Nodes;
using Bodu.Test;
using Bodu.Test.Kat;

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies that <see cref="CompoundFileBuilder.Load(Stream, CompoundFileBuilderOptions)" /> materializes a compound
/// file into an equivalent mutable object model.
/// </summary>
[TestClass]
public class CompoundFileBuilderLoadTests
{
    /// <summary>
    /// Verifies that loading a nested fixture reproduces its storage hierarchy and stream content.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Load_WhenNestedFixture_ShouldReproduceHierarchy()
    {
        using MemoryStream source = CompoundFixtures.OpenReference("valid/clean.dat");

        CompoundStorageNode root = CompoundFileBuilder.Load(source).Root;

        Assert.AreEqual(CompoundEntryType.RootStorage, root.EntryType);
        Assert.IsTrue(root.TryGetStorage("Storage 1", out CompoundStorageNode? storage));
        Assert.IsTrue(storage.TryGetStream("Stream 1", out _));
    }

    /// <summary>
    /// Verifies that, for every valid reference fixture, the loaded object model exposes the same set of stream paths
    /// and identical stream content (by SHA-256) as the read-only reader.
    /// </summary>
    /// <param name="kat">The reference fixture row.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(CompoundReferenceManifest.ValidFixtures), typeof(CompoundReferenceManifest),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Load_WhenValidFixture_ShouldMatchReaderStreamHashes(CompoundReferenceFixtureKat kat)
    {
        using MemoryStream source = CompoundFixtures.OpenReference(kat.RelativePath);
        CompoundStorageNode root = CompoundFileBuilder.Load(source).Root;

        Dictionary<string, string> actual = new(StringComparer.Ordinal);
        Collect(root, string.Empty, actual);

        foreach (CompoundManifestEntry expected in kat.Entries)
        {
            if (expected.Sha256 is null)
                continue;

            Assert.IsTrue(actual.TryGetValue(expected.Path, out string? hash), $"Missing stream: {expected.Path}");
            Assert.AreEqual(expected.Sha256, hash, expected.Path);
        }
    }

    /// <summary>
    /// Recursively records the SHA-256 of every stream in the object model, keyed by storage-qualified path.
    /// </summary>
    /// <param name="storage">The storage to walk.</param>
    /// <param name="prefix">The path prefix for the current storage.</param>
    /// <param name="hashes">The map receiving path-to-hash entries.</param>
    private static void Collect(CompoundStorageNode storage, string prefix, Dictionary<string, string> hashes)
    {
        foreach (CompoundStreamNode stream in storage.EnumerateStreams())
        {
            string path = prefix.Length == 0 ? stream.Name : prefix + "/" + stream.Name;
            hashes[path] = Convert.ToHexString(SHA256.HashData(stream.Content.Span)).ToLowerInvariant();
        }

        foreach (CompoundStorageNode child in storage.EnumerateStorages())
            Collect(child, prefix.Length == 0 ? child.Name : prefix + "/" + child.Name, hashes);
    }
}
