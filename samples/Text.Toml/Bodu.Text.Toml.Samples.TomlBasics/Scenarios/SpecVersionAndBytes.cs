// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SpecVersionAndBytes.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;
using Bodu.Text.Toml;

namespace Bodu.Text.Toml.Samples.TomlBasics.Scenarios;

/// <summary>
/// Demonstrates the two wire-level knobs: <c>SpecVersion</c> gates the TOML v1.1.0 grammar
/// extensions on parse (v1.0.0 stays the strict default), and <c>ByteArrayHandling</c> chooses
/// how <c>byte[]</c> travels - an integer array or a Base64 string.
/// </summary>
public static class SpecVersionAndBytes
{
    /// <summary>
    /// A payload POCO carrying raw bytes.
    /// </summary>
    private sealed class Packet
    {
        public string Kind { get; set; } = "checksum";

        public byte[] Payload { get; set; } = [0xDE, 0xAD, 0xBE, 0xEF];
    }

    /// <summary>
    /// Parses a v1.1-only document under both spec versions, then serializes bytes both ways.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Spec version gating and byte-array wire shapes",
            what: "Parses a document using a TOML v1.1 escape under the default spec version and again with v1.1 "
                + "enabled, then serializes the same byte array under both wire shapes and round-trips one of "
                + "them.",
            why: "Pinning the spec version is a compatibility decision that runs in both directions. Accepting "
                + "v1.1 grammar by default would mean this library happily reads documents that other TOML "
                + "parsers reject, so a config file that works here fails in the next tool that touches it - "
                + "which is why v1.0.0 is the default and the newer grammar is opt-in. Byte arrays are a "
                + "different kind of choice: TOML has no binary type, so the bytes have to be encoded as "
                + "something, and neither answer is universally right. An integer array is readable and "
                + "editable by hand; Base64 is compact and does not turn a kilobyte into a wall of numbers.",
            expect: "The same document is rejected under the default and accepted under v1.1, which is the gate "
                + "working - the parser is strict by default rather than permissive. The two byte shapes are "
                + "visibly different encodings of identical bytes, and the Base64 form reads back to the "
                + "original payload under the default handling, because the option shapes only what is "
                + "written: a string and an integer array are told apart on the wire.");

        // TOML v1.1.0 allows \x hex escapes in strings; v1.0.0 (the default) does not.
        var v11Document = "greeting = \"caf\\xe9\"";

        try
        {
            TomlSerializer.Deserialize<Dictionary<string, string>>(v11Document);
        }
        catch (TomlFormatException ex)
        {
            Console.WriteLine($"  V1_0 (default): rejected -> {ex.Message}"
                + "  (strict by default, so a document accepted here is one other TOML parsers will also accept)");
        }

        var v11Options = new TomlSerializerOptions { SpecVersion = TomlSpecVersion.V1_1 };
        var parsed = TomlSerializer.Deserialize<Dictionary<string, string>>(v11Document, v11Options);
        Console.WriteLine($"  V1_1          : accepted -> greeting = \"{parsed["greeting"]}\""
            + "  (the newer grammar is opt-in - raising the version is a deliberate compatibility decision)");

        // byte[] wire shape: integer array (default, self-describing) vs Base64 (compact).
        var packet = new Packet();
        var asIntegers = TomlSerializer.Serialize(packet, new TomlSerializerOptions
        {
            PropertyNamingPolicy = NamingPolicy.SnakeCaseLower,
            ByteArrayHandling = TomlByteArrayHandling.IntegerArray,
        });
        var asBase64 = TomlSerializer.Serialize(packet, new TomlSerializerOptions
        {
            PropertyNamingPolicy = NamingPolicy.SnakeCaseLower,
            ByteArrayHandling = TomlByteArrayHandling.Base64String,
        });

        Console.WriteLine($"  IntegerArray  : {asIntegers.Split('\n').First(l => l.StartsWith("payload", StringComparison.Ordinal)).TrimEnd()}"
            + "  (readable and hand-editable, but a kilobyte of payload becomes a wall of numbers)");
        Console.WriteLine($"  Base64String  : {asBase64.Split('\n').First(l => l.StartsWith("payload", StringComparison.Ordinal)).TrimEnd()}"
            + "  (compact, but opaque to a human editing the file - TOML has no binary type, so one of these has to be chosen)");

        // ByteArrayHandling governs writing only: on read a string decodes as Base64 and an integer array
        // as bytes, whatever the option says, so the Base64 form reads back under the default handling.
        var roundTripped = TomlSerializer.Deserialize<Packet>(asBase64, new TomlSerializerOptions
        {
            PropertyNamingPolicy = NamingPolicy.SnakeCaseLower,
        });
        Console.WriteLine($"  Round trip    : payload restored -> {Convert.ToHexString(roundTripped.Payload)}"
            + "  (read under the default IntegerArray handling - the option shapes writing only, and the reader accepts either form)");

        Console.WriteLine();
    }
}
