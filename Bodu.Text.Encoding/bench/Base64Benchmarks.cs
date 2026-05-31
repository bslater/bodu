// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Benchmarks.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using BenchmarkDotNet.Attributes;

namespace Bodu.Text.Encoding.Benchmarks;

/// <summary>
/// Measures the throughput of <see cref="Base64" /> encode and decode at sizes that span the small-input
/// stackalloc threshold up to multi-megabyte payloads.
/// </summary>
[MemoryDiagnoser]
public class Base64Benchmarks
{
    private byte[] _payload = Array.Empty<byte>();
    private string _encoded = string.Empty;

    /// <summary>
    /// Gets or sets the size, in bytes, of the input buffer encoded by each benchmark iteration.
    /// </summary>
    [Params(32, 192, 1024, 65536, 1048576)]
    public int PayloadSize { get; set; }

    /// <summary>
    /// Allocates the input buffer and pre-encodes it once so decode benchmarks can run without setup overhead.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _payload = new byte[PayloadSize];
        new Random(20260524).NextBytes(_payload);
        _encoded = Base64.Encode(_payload);
    }

    /// <summary>
    /// Benchmarks <see cref="Base64.Encode(byte[], Base64Variant)" /> for the configured payload size.
    /// </summary>
    /// <returns>The encoded string.</returns>
    [Benchmark]
    public string Encode_Standard() =>
        Base64.Encode(_payload);

    /// <summary>
    /// Benchmarks <see cref="Base64.Decode(string, Base64Variant, BaseFormatStyles)" /> for the configured payload
    /// size.
    /// </summary>
    /// <returns>The decoded bytes.</returns>
    [Benchmark]
    public byte[] Decode_Standard() =>
        Base64.Decode(_encoded);

    /// <summary>
    /// Benchmarks <see cref="Base64Url.Encode(byte[])" /> for the configured payload size.
    /// </summary>
    /// <returns>The encoded string.</returns>
    [Benchmark]
    public string Encode_UrlSafe() =>
        Base64Url.Encode(_payload);
}
