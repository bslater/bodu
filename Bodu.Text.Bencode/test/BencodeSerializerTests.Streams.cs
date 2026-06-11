// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.Streams.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the <see cref="Stream" />-based <see cref="BencodeSerializer" /> overloads: asynchronous serialization
/// writes the same canonical bytes as the in-memory overload, and both the synchronous and asynchronous stream
/// deserialization overloads round-trip a value.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that <see cref="BencodeSerializer.SerializeAsync{T}(Stream, T, BencodeSerializerOptions?,
    /// CancellationToken)" /> writes the same canonical bytes to the stream as the in-memory overload returns.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SerializeAsync_WhenStreamDestination_ShouldWriteCanonicalBytes()
    {
        var model = new StreamModel { Id = 7, Label = "x" };
        using var destination = new MemoryStream();

        await BencodeSerializer.SerializeAsync(destination, model);

        CollectionAssert.AreEqual(BencodeSerializer.Serialize(model), destination.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeSerializer.Deserialize{T}(Stream, BencodeSerializerOptions?)" /> reads a value
    /// from a stream positioned at a canonical document.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStreamSource_ShouldReturnValue()
    {
        using var source = new MemoryStream(Encoding.Latin1.GetBytes("d2:Idi7e5:Label1:xe"));

        StreamModel model = BencodeSerializer.Deserialize<StreamModel>(source);

        Assert.AreEqual(7, model.Id);
        Assert.AreEqual("x", model.Label);
    }

    /// <summary>
    /// Verifies that a value serialized to a stream with <see cref="BencodeSerializer.SerializeAsync{T}(Stream, T,
    /// BencodeSerializerOptions?, CancellationToken)" /> reads back equal through
    /// <see cref="BencodeSerializer.DeserializeAsync{T}(Stream, BencodeSerializerOptions?, CancellationToken)" />.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsync_WhenStreamRoundTrip_ShouldReturnEqualValue()
    {
        var original = new StreamModel { Id = 42, Label = "héllo" };
        using var stream = new MemoryStream();

        await BencodeSerializer.SerializeAsync(stream, original);
        stream.Position = 0;
        StreamModel roundTripped = await BencodeSerializer.DeserializeAsync<StreamModel>(stream);

        Assert.AreEqual(original.Id, roundTripped.Id);
        Assert.AreEqual(original.Label, roundTripped.Label);
    }

    /// <summary>
    /// A small model used by the stream overload tests.
    /// </summary>
    private sealed class StreamModel
    {
        /// <summary>Gets or sets the identifier.</summary>
        /// <returns>The identifier.</returns>
        public int Id { get; set; }

        /// <summary>Gets or sets the label.</summary>
        /// <returns>The label.</returns>
        public string Label { get; set; } = string.Empty;
    }
}
