// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SpecVersionAndBytes.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml;

namespace Bodu.Text.Toml.Samples.TomlBasics.Scenarios;

/// <summary>
/// Demonstrates the two wire-level knobs: <c>SpecVersion</c> gates the TOML v1.1.0 grammar
/// extensions on parse (v1.0.0 stays the strict default), and <c>ByteArrayHandling</c> chooses
/// how <c>byte[]</c> travels — an integer array or a Base64 string.
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
        Console.WriteLine("--- TomlSpecVersion and TomlByteArrayHandling ---");

        // TOML v1.1.0 allows \x hex escapes in strings; v1.0.0 (the default) does not.
        var v11Document = "greeting = \"caf\\xe9\"";

        try
        {
            TomlSerializer.Deserialize<Dictionary<string, string>>(v11Document);
        }
        catch (TomlFormatException ex)
        {
            Console.WriteLine($"  V1_0 (default): rejected -> {ex.Message}");
        }

        var v11Options = new TomlSerializerOptions { SpecVersion = TomlSpecVersion.V1_1 };
        var parsed = TomlSerializer.Deserialize<Dictionary<string, string>>(v11Document, v11Options);
        Console.WriteLine($"  V1_1          : accepted -> greeting = \"{parsed["greeting"]}\"");

        // byte[] wire shape: integer array (default, self-describing) vs Base64 (compact).
        var packet = new Packet();
        var asIntegers = TomlSerializer.Serialize(packet, new TomlSerializerOptions
        {
            PropertyNamingPolicy = TomlNamingPolicy.SnakeCaseLower,
            ByteArrayHandling = TomlByteArrayHandling.IntegerArray,
        });
        var asBase64 = TomlSerializer.Serialize(packet, new TomlSerializerOptions
        {
            PropertyNamingPolicy = TomlNamingPolicy.SnakeCaseLower,
            ByteArrayHandling = TomlByteArrayHandling.Base64String,
        });

        Console.WriteLine($"  IntegerArray  : {asIntegers.Split('\n').First(l => l.StartsWith("payload", StringComparison.Ordinal)).TrimEnd()}");
        Console.WriteLine($"  Base64String  : {asBase64.Split('\n').First(l => l.StartsWith("payload", StringComparison.Ordinal)).TrimEnd()}");

        // Both shapes deserialize back to the same bytes when the handling matches.
        var roundTripped = TomlSerializer.Deserialize<Packet>(asBase64, new TomlSerializerOptions
        {
            PropertyNamingPolicy = TomlNamingPolicy.SnakeCaseLower,
            ByteArrayHandling = TomlByteArrayHandling.Base64String,
        });
        Console.WriteLine($"  Round trip    : payload restored -> {Convert.ToHexString(roundTripped.Payload)}");

        Console.WriteLine();
    }
}
