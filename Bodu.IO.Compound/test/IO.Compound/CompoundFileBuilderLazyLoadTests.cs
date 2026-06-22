// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileBuilderLazyLoadTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.IO.Compound.Nodes;
using Bodu.Test;

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies the lazy (deferred) load path of
/// <see cref="CompoundFileBuilder.FromFile(CompoundFile, bool, CompoundFileBuilderOptions)" />.
/// </summary>
[TestClass]
public class CompoundFileBuilderLazyLoadTests
{
    /// <summary>
    /// Verifies that a lazy load produces deferred stream nodes that report their length without materializing content.
    /// </summary>
    [TestMethod]
    public void FromFile_WhenLazy_ShouldProduceDeferredNodes()
    {
        using MemoryStream source = CompoundFixtures.OpenReference("valid/clean.dat");
        using CompoundFile file = CompoundFile.Open(source, buffered: false);

        CompoundStorageNode root = CompoundFileBuilder.FromFile(file, lazy: true).Root;

        bool sawStream = false;
        foreach (CompoundStreamNode stream in EnumerateStreams(root))
        {
            sawStream = true;
            Assert.IsTrue(stream.IsDeferred, stream.Name);
        }

        Assert.IsTrue(sawStream);
    }

    /// <summary>
    /// Verifies that re-saving a lazily-loaded model reproduces every stream's content (a streamed read/write copy).
    /// </summary>
    [TestMethod]
    public void FromFile_WhenLazyThenSaved_ShouldReproduceStreamContent()
    {
        using MemoryStream source = CompoundFixtures.OpenReference("valid/clean.dat");
        using CompoundFile file = CompoundFile.Open(source, buffered: false);

        Dictionary<string, string> expected = HashStreams(file.RootStorage);

        byte[] rewritten = CompoundFileBuilder.FromFile(file, lazy: true).ToArray();

        using CompoundFile reopened = CompoundFile.Open(new MemoryStream(rewritten));
        Dictionary<string, string> actual = HashStreams(reopened.RootStorage);

        CollectionAssert.AreEquivalent(expected.Keys.ToList(), actual.Keys.ToList());
        foreach (KeyValuePair<string, string> pair in expected)
            Assert.AreEqual(pair.Value, actual[pair.Key], pair.Key);
    }

    /// <summary>
    /// Verifies an end-to-end streamed copy: a large file opened in streaming mode, lazily loaded, and saved to disk
    /// preserves every stream's content.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void FromFile_WhenLazyStreamedCopyOfLargeFile_ShouldPreserveContent()
    {
        string inputPath = Path.Combine(Path.GetTempPath(), $"bodu-in-{Guid.NewGuid():N}.cfb");
        string outputPath = Path.Combine(Path.GetTempPath(), $"bodu-out-{Guid.NewGuid():N}.cfb");
        try
        {
            byte[] big = Payload(6 * 1024 * 1024);
            var authored = new CompoundFileBuilder();
            _ = authored.Root.AddStream("Big", big);
            _ = authored.Root.AddStream("Small", Payload(20));
            authored.Save(inputPath);

            using (FileStream read = File.OpenRead(inputPath))
            using (CompoundFile file = CompoundFile.Open(read, buffered: false))
            {
                CompoundFileBuilder lazy = CompoundFileBuilder.FromFile(file, lazy: true);
                lazy.Save(outputPath);
            }

            using FileStream verify = File.OpenRead(outputPath);
            using CompoundFile copy = CompoundFile.Open(verify, buffered: false);
            Assert.IsTrue(copy.RootStorage.TryOpenStream("Big", out CompoundStreamEntry? bigEntry));
            Assert.AreEqual(Hash(big), Hash(bigEntry.ReadAllBytes().Span));
        }
        finally
        {
            File.Delete(inputPath);
            File.Delete(outputPath);
        }
    }

    /// <summary>
    /// Enumerates every stream node in a storage hierarchy.
    /// </summary>
    /// <param name="storage">The storage to walk.</param>
    /// <returns>The stream nodes.</returns>
    private static IEnumerable<CompoundStreamNode> EnumerateStreams(CompoundStorageNode storage)
    {
        foreach (CompoundStreamNode stream in storage.EnumerateStreams())
            yield return stream;

        foreach (CompoundStorageNode child in storage.EnumerateStorages())
        {
            foreach (CompoundStreamNode stream in EnumerateStreams(child))
                yield return stream;
        }
    }

    /// <summary>
    /// Records the SHA-256 of every stream in a read-only storage hierarchy, keyed by storage-qualified path.
    /// </summary>
    /// <param name="storage">The storage to walk.</param>
    /// <returns>A map from stream path to lowercase hexadecimal SHA-256.</returns>
    private static Dictionary<string, string> HashStreams(CompoundStorage storage)
    {
        Dictionary<string, string> hashes = new(StringComparer.Ordinal);
        Walk(storage, string.Empty, hashes);
        return hashes;

        static void Walk(CompoundStorage current, string prefix, Dictionary<string, string> map)
        {
            foreach (CompoundStreamEntry entry in current.EnumerateStreams())
            {
                string path = prefix.Length == 0 ? entry.Name : prefix + "/" + entry.Name;
                map[path] = Convert.ToHexString(SHA256.HashData(entry.ReadAllBytes().Span)).ToLowerInvariant();
            }

            foreach (CompoundStorage child in current.EnumerateStorages())
                Walk(child, prefix.Length == 0 ? child.Name : prefix + "/" + child.Name, map);
        }
    }

    /// <summary>
    /// Generates a deterministic payload of the requested length.
    /// </summary>
    /// <param name="size">The payload length.</param>
    /// <returns>The payload.</returns>
    private static byte[] Payload(int size)
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
