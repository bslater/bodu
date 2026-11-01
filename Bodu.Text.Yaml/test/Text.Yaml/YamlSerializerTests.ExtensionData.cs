// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.ExtensionData.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the extension-data contract: a member marked <see cref="ExtensionDataAttribute" /> captures YAML keys that
/// map to no declared member during deserialization and writes those entries back during serialization, and misuse of
/// the attribute (a wrong member type, or more than one) is rejected.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>
    /// Verifies that keys with no matching member are captured into the extension-data member rather than skipped.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenUnmappedKeys_ShouldCaptureIntoExtensionData()
    {
        ExtensionModel model = YamlSerializer.Deserialize<ExtensionModel>("Known: 1\nfoo: bar\nnum: 3\n")!;

        Assert.AreEqual(1, model.Known);
        Assert.AreEqual("bar", model.Extra["foo"]);
        Assert.AreEqual(3L, model.Extra["num"]);
        Assert.IsFalse(model.Extra.ContainsKey("Known"));
    }

    /// <summary>
    /// Verifies that the extension-data entries are emitted after the declared members on serialization.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenExtensionData_ShouldEmitEntriesAfterMembers()
    {
        var model = new ExtensionModel { Known = 1 };
        model.Extra["foo"] = "bar";

        string yaml = YamlSerializer.Serialize(model);

        Assert.AreEqual("Known: 1\nfoo: bar\n", yaml);
    }

    /// <summary>
    /// Verifies that an extension-data key equal to a declared member's wire name throws
    /// <see cref="YamlSerializationException" /> rather than emitting a duplicate key.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenExtensionKeyCollidesWithMember_ShouldThrowYamlSerializationException()
    {
        var model = new ExtensionModel { Known = 1 };
        model.Extra["Known"] = 2;

        _ = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Serialize(model);
        });
    }

    /// <summary>
    /// Verifies that an extension-data member declared with an unsupported type throws
    /// <see cref="YamlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenExtensionDataMemberHasWrongType_ShouldThrowYamlSerializationException()
    {
        _ = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<BadExtensionTypeModel>("Known: 1\n");
        });
    }

    /// <summary>
    /// Verifies that declaring more than one extension-data member throws <see cref="YamlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenMultipleExtensionDataMembers_ShouldThrowYamlSerializationException()
    {
        _ = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<MultipleExtensionModel>("Known: 1\n");
        });
    }

    /// <summary>A model with an extension-data member that captures unmapped keys.</summary>
    private sealed class ExtensionModel
    {
        /// <summary>Gets or sets the declared member.</summary>
        /// <value>The known value.</value>
        public int Known { get; set; }

        /// <summary>Gets the captured unmapped entries.</summary>
        /// <value>The extension-data entries.</value>
        [ExtensionData]
        public Dictionary<string, object?> Extra { get; set; } = new();
    }

    /// <summary>A model whose extension-data member is declared with an unsupported type.</summary>
    private sealed class BadExtensionTypeModel
    {
        /// <summary>Gets or sets the declared member.</summary>
        /// <value>The known value.</value>
        public int Known { get; set; }

        /// <summary>Gets or sets a mistyped extension-data member.</summary>
        /// <value>The mistyped value.</value>
        [ExtensionData]
        public string? Bad { get; set; }
    }

    /// <summary>A model that wrongly declares two extension-data members.</summary>
    private sealed class MultipleExtensionModel
    {
        /// <summary>Gets or sets the declared member.</summary>
        /// <value>The known value.</value>
        public int Known { get; set; }

        /// <summary>Gets or sets the first extension-data member.</summary>
        /// <value>The first extension-data member.</value>
        [ExtensionData]
        public Dictionary<string, object?>? First { get; set; }

        /// <summary>Gets or sets the second extension-data member.</summary>
        /// <value>The second extension-data member.</value>
        [ExtensionData]
        public Dictionary<string, object?>? Second { get; set; }
    }

    /// <summary>
    /// Verifies that a public field opted in with <see cref="IncludeAttribute" /> and marked
    /// <see cref="ExtensionDataAttribute" /> captures the unmapped keys on read, as an extension-data property does.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenUnmappedKeysAndExtensionDataIsAField_ShouldCaptureIntoField()
    {
        FieldExtensionModel model = YamlSerializer.Deserialize<FieldExtensionModel>("Known: 1\nfoo: bar\n")!;

        Assert.AreEqual(1, model.Known);
        Assert.IsNotNull(model.Extra);
        Assert.AreEqual("bar", model.Extra["foo"]);
        Assert.IsFalse(model.Extra.ContainsKey("Known"));
    }

    /// <summary>
    /// Verifies that an extension-data field declared with an unsupported type throws
    /// <see cref="YamlSerializationException" />, as an unsupported extension-data property does.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenExtensionDataFieldHasWrongType_ShouldThrowYamlSerializationException()
    {
        _ = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<BadFieldExtensionTypeModel>("Known: 1\n");
        });
    }

    /// <summary>
    /// Verifies that an extension-data field beside an extension-data property throws
    /// <see cref="YamlSerializationException" />, as a second extension-data property does.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenExtensionDataFieldDuplicatesProperty_ShouldThrowYamlSerializationException()
    {
        _ = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<DuplicateFieldExtensionModel>("Known: 1\n");
        });
    }

    /// <summary>A model whose included extension-data field captures unmapped keys.</summary>
    private sealed class FieldExtensionModel
    {
        /// <summary>The captured unmapped entries, empty until the read fills it.</summary>
        [Include]
        [ExtensionData]
        public Dictionary<string, object?>? Extra = [];

        /// <summary>Gets or sets the declared member.</summary>
        /// <value>The known value.</value>
        public int Known { get; set; }
    }

    /// <summary>A model whose extension-data field is declared with an unsupported type.</summary>
    private sealed class BadFieldExtensionTypeModel
    {
        /// <summary>The mistyped extension-data field.</summary>
        [Include]
        [ExtensionData]
        public string? Bad = string.Empty;

        /// <summary>Gets or sets the declared member.</summary>
        /// <value>The known value.</value>
        public int Known { get; set; }
    }

    /// <summary>A model that wrongly declares an extension-data property and an extension-data field.</summary>
    private sealed class DuplicateFieldExtensionModel
    {
        /// <summary>The second extension-data member, a field.</summary>
        [Include]
        [ExtensionData]
        public Dictionary<string, object?>? Second = [];

        /// <summary>Gets or sets the declared member.</summary>
        /// <value>The known value.</value>
        public int Known { get; set; }

        /// <summary>Gets or sets the first extension-data member.</summary>
        /// <value>The first extension-data member.</value>
        [ExtensionData]
        public Dictionary<string, object?>? First { get; set; }
    }
}
