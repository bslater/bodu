// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerOptionsTests.Converters.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Serialization;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that the <see cref="BencodeSerializerOptions.Converters" /> collection honors the read-only state of its
/// owning options instance, so the converter set cannot change after the options are frozen or first used.
/// </summary>
public partial class BencodeSerializerOptionsTests
{
    /// <summary>Verifies that adding a converter after <see cref="BencodeSerializerOptions.MakeReadOnly" /> throws.</summary>
    [TestMethod]
    public void Converters_WhenAddedAfterMakeReadOnly_ShouldThrowInvalidOperationException()
    {
        var options = new BencodeSerializerOptions();
        options.MakeReadOnly();

        Assert.ThrowsExactly<InvalidOperationException>(() => options.Converters.Add(new PassthroughConverter()));
    }

    /// <summary>Verifies that removing a converter after the options are used to serialize throws.</summary>
    [TestMethod]
    public void Converters_WhenRemovedAfterSerialize_ShouldThrowInvalidOperationException()
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new PassthroughConverter());

        _ = BencodeSerializer.Serialize(1, options);

        Assert.ThrowsExactly<InvalidOperationException>(() => options.Converters.RemoveAt(0));
    }

    /// <summary>Verifies that clearing the converters after the options are used to deserialize throws.</summary>
    [TestMethod]
    public void Converters_WhenClearedAfterDeserialize_ShouldThrowInvalidOperationException()
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new PassthroughConverter());

        _ = BencodeSerializer.Deserialize<long>("i1e"u8, options);

        Assert.ThrowsExactly<InvalidOperationException>(options.Converters.Clear);
    }

    /// <summary>Verifies that converters can still be added before the options instance is used.</summary>
    [TestMethod]
    public void Converters_WhenAddedBeforeUse_ShouldSucceed()
    {
        var options = new BencodeSerializerOptions();
        options.Converters.Add(new PassthroughConverter());

        Assert.HasCount(1, options.Converters);
    }

    /// <summary>A converter that does not change behavior; used only to populate the converter collection.</summary>
    private sealed class PassthroughConverter
        : BencodeConverter<string>
    {
        /// <inheritdoc />
        public override string Read(ref Utf8BencodeReader reader, Type typeToConvert, BencodeSerializerOptions options) =>
            reader.GetString();

        /// <inheritdoc />
        public override void Write(Utf8BencodeWriter writer, string value, BencodeSerializerOptions options) =>
            writer.WriteString(value);
    }
}
