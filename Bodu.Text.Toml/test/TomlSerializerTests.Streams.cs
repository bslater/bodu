// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.Streams.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the <see cref="Stream" />-based <see cref="TomlSerializer" /> overloads: asynchronous serialization writes
/// the same canonical UTF-8 text as the in-memory overload, and both the synchronous and asynchronous stream
/// deserialization overloads round-trip a value.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that <see cref="TomlSerializer.SerializeAsync{T}(Stream, T, TomlSerializerOptions?,
    /// CancellationToken)" /> writes the same canonical UTF-8 text to the stream as the in-memory overload returns.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SerializeAsync_WhenStreamDestination_ShouldWriteCanonicalText()
    {
        var model = new StreamModel { Id = 7, Label = "x" };
        using var destination = new MemoryStream();

        await TomlSerializer.SerializeAsync(destination, model);

        Assert.AreEqual(TomlSerializer.Serialize(model), s_utf8.GetString(destination.ToArray()));
    }

    /// <summary>
    /// Verifies that <see cref="TomlSerializer.Deserialize{T}(Stream, TomlSerializerOptions?)" /> reads a value from a
    /// stream positioned at a canonical document.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStreamSource_ShouldReturnValue()
    {
        using var source = new MemoryStream(Encoding.UTF8.GetBytes("Id = 7\nLabel = \"x\"\n"));

        StreamModel model = TomlSerializer.Deserialize<StreamModel>(source);

        Assert.AreEqual(7, model.Id);
        Assert.AreEqual("x", model.Label);
    }

    /// <summary>
    /// Verifies that a value serialized to a stream with <see cref="TomlSerializer.SerializeAsync{T}(Stream, T,
    /// TomlSerializerOptions?, CancellationToken)" /> reads back equal through
    /// <see cref="TomlSerializer.DeserializeAsync{T}(Stream, TomlSerializerOptions?, CancellationToken)" />.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsync_WhenStreamRoundTrip_ShouldReturnEqualValue()
    {
        var original = new StreamModel { Id = 42, Label = "héllo" };
        using var stream = new MemoryStream();

        await TomlSerializer.SerializeAsync(stream, original);
        stream.Position = 0;
        StreamModel roundTripped = await TomlSerializer.DeserializeAsync<StreamModel>(stream);

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
