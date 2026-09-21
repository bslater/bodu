// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamsAndEntries.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound;

namespace Bodu.IO.Compound.Samples.CompoundBasics.Scenarios;

/// <summary>
/// Demonstrates reading a real-world container: the committed <c>sample1.doc</c> is a Word
/// 97-2003 file, which is exactly an OLE2 container. The sample walks its storage tree with
/// <see cref="CompoundStorage" />, shows per-entry metadata from <see cref="CompoundEntryInfo" />,
/// and opens a named stream — without knowing anything about the Word format itself.
/// </summary>
public static class StreamsAndEntries
{
    /// <summary>
    /// Walks the committed <c>.doc</c> fixture's tree and reads a stream's head bytes.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Walking a real document's storage tree",
            what: "Opens a committed .doc file and walks its storage hierarchy, reporting each entry's name, "
                + "type, size and colour, then reads one named stream.",
            why: "A compound file is a filesystem inside a file - directories, named streams, a FAT - and the "
                + "reason to have a reader for it is that .xls, .doc and .msg are all applications of it. "
                + "Separating the container from the application format is what lets one implementation serve "
                + "all three, and it means this layer can be correct about the envelope without knowing anything "
                + "about what the streams contain. The entry colour is exposed because the directory is a "
                + "red-black tree and a malformed colouring is a real corruption signal, not an implementation "
                + "detail.",
            expect: "The tree structure and the stream names come out of the container with no knowledge of Word "
                + "at all - the reader sees named streams, not a document. Reading one stream is a stream read, "
                + "so a large embedded object does not have to be materialized to inspect the file.");

        using var file = CompoundFile.OpenRead(Path.Combine(AppContext.BaseDirectory, "Data", "sample1.doc"));

        // Recursively dump the tree - storages are directories, streams are files.
        Dump(file.RootStorage, indent: 1);

        // Open a named stream and peek at its head - raw bytes, no Word knowledge needed.
        if (file.RootStorage.TryOpenStream("WordDocument", out var word))
        {
            using (word)
            {
                var head = new byte[8];
                var read = word.Read(head, 0, head.Length);
                Console.WriteLine($"'WordDocument' head bytes: {Convert.ToHexString(head.AsSpan(0, read))} ({word.Length} bytes total)");
            }
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Prints a storage's entries, recursing into child storages.
    /// </summary>
    private static void Dump(CompoundStorage storage, int indent)
    {
        foreach (var entry in storage.EnumerateEntries())
        {
            // Well-known stream names start with control chars (\x05SummaryInformation,
            // \x01CompObj) - render them printably.
            var name = string.Concat(entry.Name.Select(c => char.IsControl(c) ? $"\\x{(int)c:X2}" : c.ToString()));
            Console.WriteLine($"  {new string(' ', indent * 2)}{name} ({entry.EntryType}{(entry.EntryType == CompoundEntryType.Stream ? $", {entry.Length} bytes" : string.Empty)})");
        }

        foreach (var child in storage.EnumerateStorages())
        {
            Console.WriteLine($"  {new string(' ', indent * 2)}[{child.Name}]");
            Dump(child, indent + 1);
        }
    }
}
