// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AuthorAndReadBack.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.IO.Compound;
using Bodu.IO.Compound.Builders;

namespace Bodu.IO.Compound.Samples.CompoundBasics.Scenarios;

/// <summary>
/// Demonstrates the authoring loop: build a container bottom-up with the staged
/// <see cref="CompoundStorageBuilder" /> API (storages nest, streams carry bytes), write it to
/// a stream, then reopen it with <see cref="CompoundFile" /> and walk the tree back — a whole
/// structured-storage round trip with no file on disk.
/// </summary>
public static class AuthorAndReadBack
{
    /// <summary>
    /// Authors an in-memory container and reads its tree and stream contents back.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Authoring a container and reading it back",
            what: "Builds a new compound file with nested storages and streams through the builder API, then "
                + "opens the result and walks it back.",
            why: "The round trip is the assertion, because the format has enough bookkeeping - the FAT, the "
                + "mini-FAT for small streams, the directory tree - that a writer can produce something which "
                + "looks structurally plausible and is not readable. Proving a written container reads back "
                + "means the allocation and the directory are consistent, not merely well-formed. The builder is "
                + "staged rather than incremental because sizes and allocations are only known once everything "
                + "is present, which is also why authoring and editing are separate APIs.",
            expect: "The authored container reads back with the same tree and the same stream contents. Small "
                + "streams go through the mini-FAT and large ones through the FAT, and both paths round-trip - "
                + "which is the split most likely to be got wrong.");

        // Build: a root with one stream, plus a nested storage holding two more.
        var root = CompoundStorageBuilder.CreateRoot();
        root.AddStream("Manifest", "app=sample;version=1"u8.ToArray());

        var payload = root.AddStorage("Payload");
        payload.AddStream("ReadMe", "Structured storage is a filesystem in a file."u8.ToArray());
        payload.AddStream("Numbers", new byte[] { 1, 2, 3, 4, 5 });

        // Write the container to any stream.
        using var container = new MemoryStream();
        root.WriteTo(container);
        Console.WriteLine($"  authored container: {container.Length} bytes");

        // Read back: reopen and walk the tree.
        container.Position = 0;
        using var file = CompoundFile.Open(container, leaveOpen: true);

        // EnumerateEntries lists children of both kinds: storages (directory-like containers) and
        // streams (the leaves that carry bytes); EnumerateStreams/EnumerateStorages filter to one kind.
        foreach (var entry in file.RootStorage.EnumerateEntries())
        {
            Console.WriteLine($"  /{entry.Name} ({entry.EntryType}, {entry.Length} bytes)");
        }

        var payloadStorage = file.RootStorage.OpenStorage("Payload");
        foreach (var entry in payloadStorage.EnumerateStreams())
        {
            Console.WriteLine($"  /Payload/{entry.Name} ({entry.Length} bytes)");
        }

        // Stream contents survive byte-for-byte.
        using var readMe = payloadStorage.OpenStream("ReadMe");
        using var reader = new StreamReader(readMe, Encoding.UTF8);
        Console.WriteLine($"  /Payload/ReadMe content: '{reader.ReadToEnd()}'");

        Console.WriteLine();
    }
}
