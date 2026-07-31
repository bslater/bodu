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
        Console.WriteLine("--- Author with CompoundStorageBuilder, read back with CompoundFile ---");

        // Build: a root with one stream, plus a nested storage holding two more.
        var root = CompoundStorageBuilder.CreateRoot();
        root.AddStream("Manifest", "app=sample;version=1"u8.ToArray());

        var payload = root.AddStorage("Payload");
        payload.AddStream("ReadMe", "Structured storage is a filesystem in a file."u8.ToArray());
        payload.AddStream("Numbers", new byte[] { 1, 2, 3, 4, 5 });

        // Write the container to any stream.
        using var container = new MemoryStream();
        root.WriteTo(container);
        Console.WriteLine($"authored container: {container.Length} bytes");

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
