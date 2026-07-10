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
        Console.WriteLine("--- Walk a real .doc's storage tree ---");

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
            Console.WriteLine($"{new string(' ', indent * 2)}{name} ({entry.EntryType}{(entry.EntryType == CompoundEntryType.Stream ? $", {entry.Length} bytes" : string.Empty)})");
        }

        foreach (var child in storage.EnumerateStorages())
        {
            Console.WriteLine($"{new string(' ', indent * 2)}[{child.Name}]");
            Dump(child, indent + 1);
        }
    }
}
