// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundReferenceRoundTripTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.IO.Compound.Nodes;
using Bodu.Test;
using Bodu.Test.Kat;

namespace Bodu.IO.Compound.Writer;

/// <summary>
/// Verifies that loading a real compound file into the object model, re-serializing it, and reading it back preserves
/// every stream exactly (an idempotent round-trip).
/// </summary>
[TestClass]
public class CompoundReferenceRoundTripTests
{
    /// <summary>
    /// Verifies that each valid reference fixture survives a load/save/reload cycle with identical stream content.
    /// </summary>
    /// <param name="kat">The reference fixture row.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(CompoundReferenceManifest.ValidFixtures), typeof(CompoundReferenceManifest),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Save_WhenReloadingValidFixture_ShouldPreserveStreamContent(CompoundReferenceFixtureKat kat)
    {
        CompoundStorageNode root;
        using (MemoryStream source = CompoundFixtures.OpenReference(kat.RelativePath))
            root = CompoundStorageNode.Load(source);

        byte[] rewritten = root.ToArray();

        using CompoundFile file = CompoundFile.Open(new MemoryStream(rewritten));
        Dictionary<string, string> actual = new(StringComparer.Ordinal);
        Collect(file.RootStorage, string.Empty, actual);

        foreach (CompoundManifestEntry expected in kat.Entries)
        {
            if (expected.Sha256 is null)
                continue;

            Assert.IsTrue(actual.TryGetValue(expected.Path, out string? hash), $"Missing stream: {expected.Path}");
            Assert.AreEqual(expected.Sha256, hash, expected.Path);
        }
    }

    /// <summary>
    /// Records the SHA-256 of every stream in a storage hierarchy, keyed by storage-qualified path.
    /// </summary>
    /// <param name="storage">The storage to walk.</param>
    /// <param name="prefix">The path prefix.</param>
    /// <param name="hashes">The map receiving the entries.</param>
    private static void Collect(CompoundStorage storage, string prefix, Dictionary<string, string> hashes)
    {
        foreach (CompoundStreamEntry stream in storage.EnumerateStreams())
        {
            string path = prefix.Length == 0 ? stream.Name : prefix + "/" + stream.Name;
            hashes[path] = Convert.ToHexString(SHA256.HashData(stream.ReadAllBytes().Span)).ToLowerInvariant();
        }

        foreach (CompoundStorage child in storage.EnumerateStorages())
            Collect(child, prefix.Length == 0 ? child.Name : prefix + "/" + child.Name, hashes);
    }
}
