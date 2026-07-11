// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerOptionsTests.Converters.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Serialization;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that the <see cref="YamlSerializerOptions.Converters" /> collection honors the read-only state of its
/// owning options instance, so the converter set cannot change after the options are frozen or first used.
/// </summary>
public partial class YamlSerializerOptionsTests
{
    /// <summary>Verifies that adding a converter after <see cref="YamlSerializerOptions.MakeReadOnly" /> throws.</summary>
    [TestMethod]
    public void Converters_WhenAddedAfterMakeReadOnly_ShouldThrowInvalidOperationException()
    {
        var options = new YamlSerializerOptions();
        options.MakeReadOnly();

        Assert.ThrowsExactly<InvalidOperationException>(() => options.Converters.Add(new PassthroughConverter()));
    }

    /// <summary>Verifies that removing a converter after the options are used to serialize throws.</summary>
    [TestMethod]
    public void Converters_WhenRemovedAfterSerialize_ShouldThrowInvalidOperationException()
    {
        var options = new YamlSerializerOptions();
        options.Converters.Add(new PassthroughConverter());

        _ = YamlSerializer.Serialize(1, options);

        Assert.ThrowsExactly<InvalidOperationException>(() => options.Converters.RemoveAt(0));
    }

    /// <summary>Verifies that clearing the converters after the options are used to deserialize throws.</summary>
    [TestMethod]
    public void Converters_WhenClearedAfterDeserialize_ShouldThrowInvalidOperationException()
    {
        var options = new YamlSerializerOptions();
        options.Converters.Add(new PassthroughConverter());

        _ = YamlSerializer.Deserialize<int>("1\n", options);

        Assert.ThrowsExactly<InvalidOperationException>(options.Converters.Clear);
    }

    /// <summary>Verifies that converters can still be added before the options instance is used.</summary>
    [TestMethod]
    public void Converters_WhenAddedBeforeUse_ShouldSucceed()
    {
        var options = new YamlSerializerOptions();
        options.Converters.Add(new PassthroughConverter());

        Assert.AreEqual(1, options.Converters.Count);
    }

    /// <summary>A converter that does not change behavior; used only to populate the converter collection.</summary>
    private sealed class PassthroughConverter : YamlConverter<string>
    {
        /// <inheritdoc />
        public override string Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options) => reader.GetString();

        /// <inheritdoc />
        public override void Write(Utf8YamlWriter writer, string value, YamlSerializerOptions options) =>
            writer.WriteString(value);
    }
}
