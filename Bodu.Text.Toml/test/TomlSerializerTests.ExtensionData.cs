// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.ExtensionData.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Nodes;
using Bodu.Text.Toml.Serialization;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies how <see cref="TomlSerializer" /> captures and re-emits unmatched table entries through a member annotated
/// with <see cref="TomlExtensionDataAttribute" />: overflow capture on read, write-back in document order after the
/// declared members, both the <see cref="TomlObject" /> and <c>IDictionary&lt;string, TomlNode?&gt;</c> target shapes,
/// the get-only dictionary populated in place, and the rejection of unsupported or duplicate extension-data members.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that table entries with no matching member are captured into a settable <see cref="TomlObject" />
    /// extension-data member on read.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenUnmatchedKeysAndTomlObjectExtensionData_ShouldCaptureOverflow()
    {
        var model = TomlSerializer.Deserialize<ObjectExtensionDataModel>("Name = \"n\"\nalpha = 1\nbeta = 2\n");

        Assert.AreEqual("n", model.Name);
        Assert.IsNotNull(model.Extra);
        Assert.AreEqual(2, model.Extra.Count);
        Assert.IsTrue(model.Extra.ContainsKey("alpha"));
        Assert.IsTrue(model.Extra.ContainsKey("beta"));
    }

    /// <summary>
    /// Verifies that a captured <see cref="TomlObject" /> extension-data member is written back after the type's
    /// declared members in the order the entries were read, since TOML output preserves document order rather than
    /// sorting keys.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenTomlObjectExtensionData_ShouldWriteBackAfterDeclaredMembers()
    {
        var model = TomlSerializer.Deserialize<ObjectExtensionDataModel>("Name = \"n\"\nzzz = 9\nalpha = 1\n");

        var rewritten = TomlSerializer.Serialize(model);

        // The declared member Name comes first, then the extension entries in their captured order.
        Assert.AreEqual("Name = \"n\"\nzzz = 9\nalpha = 1\n", rewritten);
    }

    /// <summary>
    /// Verifies that unmatched table entries are captured into a settable
    /// <c>Dictionary&lt;string, TomlNode?&gt;</c> extension-data member on read.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDictionaryExtensionData_ShouldCaptureOverflow()
    {
        var model = TomlSerializer.Deserialize<DictionaryExtensionDataModel>("Name = \"n\"\nextra = 7\n");

        Assert.AreEqual("n", model.Name);
        Assert.IsNotNull(model.Extra);
        Assert.AreEqual(1, model.Extra.Count);
        Assert.IsTrue(model.Extra.ContainsKey("extra"));
    }

    /// <summary>
    /// Verifies that a get-only <c>IDictionary&lt;string, TomlNode?&gt;</c> extension-data member, initialized by the
    /// type, is populated in place with the captured entries rather than being replaced.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenGetOnlyDictionaryExtensionData_ShouldPopulateExistingInstance()
    {
        var model = TomlSerializer.Deserialize<GetOnlyDictionaryExtensionDataModel>("Name = \"n\"\nextra = 7\n");

        Assert.AreEqual("n", model.Name);
        Assert.AreEqual(1, model.Extra.Count);
        Assert.IsTrue(model.Extra.ContainsKey("extra"));
    }

    /// <summary>
    /// Verifies that a key matching a declared member is bound to that member and not captured by the extension-data
    /// member, so only genuinely unmatched keys overflow.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenKeyMatchesDeclaredMember_ShouldNotCaptureIntoExtensionData()
    {
        var model = TomlSerializer.Deserialize<ObjectExtensionDataModel>("Name = \"n\"\nextra = 7\n");

        Assert.AreEqual("n", model.Name);
        Assert.IsNotNull(model.Extra);
        Assert.IsFalse(model.Extra.ContainsKey("Name"));
        Assert.AreEqual(1, model.Extra.Count);
    }

    /// <summary>
    /// Verifies that a type with an extension-data member but no overflow entries leaves the member unset, so a
    /// document with only matched keys does not allocate extension data.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNoUnmatchedKeys_ShouldLeaveExtensionDataUnset()
    {
        var model = TomlSerializer.Deserialize<ObjectExtensionDataModel>("Name = \"n\"\n");

        Assert.AreEqual("n", model.Name);
        Assert.IsNull(model.Extra);
    }

    /// <summary>
    /// Verifies that an unmatched key is captured into extension data even when the serializer-wide unmapped-member
    /// handling is <see cref="TomlUnmappedMemberHandling.Disallow" />, because extension data takes precedence.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDisallowAndExtensionDataPresent_ShouldCaptureAndNotThrow()
    {
        var options = new TomlSerializerOptions { UnmappedMemberHandling = TomlUnmappedMemberHandling.Disallow };

        var model = TomlSerializer.Deserialize<ObjectExtensionDataModel>("Name = \"n\"\nunknown = 1\n", options);

        Assert.AreEqual("n", model.Name);
        Assert.IsNotNull(model.Extra);
        Assert.IsTrue(model.Extra.ContainsKey("unknown"));
    }

    /// <summary>
    /// Verifies that serializing a model whose extension-data member holds entries emits those entries after the
    /// declared members in the order they were added, exercising the write path directly from a constructed instance.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenExtensionDataPopulated_ShouldEmitEntriesInInsertionOrder()
    {
        var extra = new TomlObject { ["zeta"] = TomlValue.Create(1L), ["alpha"] = TomlValue.Create("a") };
        var model = new ObjectExtensionDataModel { Name = "n", Extra = extra };

        var text = TomlSerializer.Serialize(model);

        // Unlike Bencode's canonical key sort, the entries keep their insertion order: zeta before alpha.
        Assert.AreEqual("Name = \"n\"\nzeta = 1\nalpha = \"a\"\n", text);
    }

    /// <summary>
    /// Verifies that resolving a type that declares more than one extension-data member throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenMultipleExtensionDataMembers_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = TomlSerializer.Deserialize<MultipleExtensionDataModel>(string.Empty);
        });
    }

    /// <summary>
    /// Verifies that resolving a type whose extension-data member is of an unsupported type throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenExtensionDataMemberTypeUnsupported_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = TomlSerializer.Deserialize<UnsupportedExtensionDataModel>(string.Empty);
        });
    }

    /// <summary>
    /// A model whose overflow keys are captured into a settable <see cref="TomlObject" />.
    /// </summary>
    private sealed class ObjectExtensionDataModel
    {
        /// <summary>
        /// Gets or sets the declared name member.
        /// </summary>
        /// <returns>The name.</returns>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the extension-data member that captures unmatched keys.
        /// </summary>
        /// <returns>The captured entries, or <see langword="null" /> when none were read.</returns>
        [TomlExtensionData]
        public TomlObject? Extra { get; set; }
    }

    /// <summary>
    /// A model whose overflow keys are captured into a settable dictionary.
    /// </summary>
    private sealed class DictionaryExtensionDataModel
    {
        /// <summary>
        /// Gets or sets the declared name member.
        /// </summary>
        /// <returns>The name.</returns>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the extension-data member that captures unmatched keys.
        /// </summary>
        /// <returns>The captured entries, or <see langword="null" /> when none were read.</returns>
        [TomlExtensionData]
        public Dictionary<string, TomlNode?>? Extra { get; set; }
    }

    /// <summary>
    /// A model whose extension-data member is get-only and pre-initialized, populated in place on read.
    /// </summary>
    private sealed class GetOnlyDictionaryExtensionDataModel
    {
        /// <summary>
        /// Gets or sets the declared name member.
        /// </summary>
        /// <returns>The name.</returns>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets the get-only extension-data member, populated in place with unmatched keys.
        /// </summary>
        /// <returns>The captured entries.</returns>
        [TomlExtensionData]
        public IDictionary<string, TomlNode?> Extra { get; } = new Dictionary<string, TomlNode?>(StringComparer.Ordinal);
    }

    /// <summary>
    /// A model that declares two extension-data members, which is invalid.
    /// </summary>
    private sealed class MultipleExtensionDataModel
    {
        /// <summary>
        /// Gets or sets the first extension-data member.
        /// </summary>
        /// <returns>The first captured entries.</returns>
        [TomlExtensionData]
        public TomlObject? First { get; set; }

        /// <summary>
        /// Gets or sets the second extension-data member, which makes the type invalid.
        /// </summary>
        /// <returns>The second captured entries.</returns>
        [TomlExtensionData]
        public TomlObject? Second { get; set; }
    }

    /// <summary>
    /// A model whose extension-data member is of an unsupported type.
    /// </summary>
    private sealed class UnsupportedExtensionDataModel
    {
        /// <summary>
        /// Gets or sets the extension-data member declared with an unsupported value type.
        /// </summary>
        /// <returns>The captured entries.</returns>
        [TomlExtensionData]
        public Dictionary<string, int>? Extra { get; set; }
    }
}
