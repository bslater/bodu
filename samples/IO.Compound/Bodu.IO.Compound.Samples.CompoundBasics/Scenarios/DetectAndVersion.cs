// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DetectAndVersion.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound;
using Bodu.IO.Compound.Builders;

namespace Bodu.IO.Compound.Samples.CompoundBasics.Scenarios;

/// <summary>
/// Demonstrates format detection and the version knob: <see cref="CompoundFile.IsCompoundFile(ReadOnlySpan{byte})" />
/// answers "is this bytes an OLE2 container?" from the 8-byte signature without a full parse,
/// and <see cref="CompoundBuildOptions.Version" /> selects v3 (512-byte sectors) or v4
/// (4096-byte sectors) when authoring — visible directly in the emitted container size.
/// </summary>
public static class DetectAndVersion
{
    /// <summary>
    /// Probes several buffers for the CFB signature and authors v3 vs v4 containers.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Detection and the v3/v4 version knob ---");

        // Detection: the committed fixtures are containers, arbitrary bytes are not.
        var cfb = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Data", "golden-v3.cfb"));
        var doc = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Data", "sample1.doc"));
        var text = "just some text, definitely not structured storage"u8.ToArray();

        Console.WriteLine($"golden-v3.cfb : IsCompoundFile = {CompoundFile.IsCompoundFile(cfb)}");
        Console.WriteLine($"sample1.doc   : IsCompoundFile = {CompoundFile.IsCompoundFile(doc)} (a .doc IS an OLE2 container)");
        Console.WriteLine($"plain text    : IsCompoundFile = {CompoundFile.IsCompoundFile(text)}");

        // Version: the same tiny tree, authored as v3 and v4.
        var root = CompoundStorageBuilder.CreateRoot();
        root.AddStream("Tiny", "hello"u8.ToArray());

        using var v3 = new MemoryStream();
        root.WriteTo(v3, new CompoundBuildOptions { Version = CompoundFileVersion.V3 });

        using var v4 = new MemoryStream();
        root.WriteTo(v4, new CompoundBuildOptions { Version = CompoundFileVersion.V4 });

        Console.WriteLine($"same content authored as V3: {v3.Length,6} bytes (512-byte sectors)");
        Console.WriteLine($"same content authored as V4: {v4.Length,6} bytes (4096-byte sectors)");

        // Both read back through the same API.
        v4.Position = 0;
        using var reopened = CompoundFile.Open(v4, leaveOpen: true);
        Console.WriteLine($"V4 container reopens: root has {reopened.RootStorage.EnumerateEntries().Count()} entry");

        Console.WriteLine();
    }
}
