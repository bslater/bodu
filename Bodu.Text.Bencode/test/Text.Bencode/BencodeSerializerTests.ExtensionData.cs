// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.ExtensionData.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Bencode.Nodes;
using Bodu.Text.Bencode.Serialization;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies how <see cref="BencodeSerializer" /> captures and re-emits unmatched dictionary entries through a member
/// annotated with <see cref="BencodeExtensionDataAttribute" />: overflow capture on read, write-back into canonical key
/// order, both the <see cref="BencodeObject" /> and <c>IDictionary&lt;string, BencodeNode?&gt;</c> target shapes, the
/// get-only dictionary populated in place, and the rejection of unsupported or duplicate extension-data members.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that dictionary entries with no matching member are captured into a settable
    /// <see cref="BencodeObject" /> extension-data member on read.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenUnmatchedKeysAndBencodeObjectExtensionData_ShouldCaptureOverflow()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d4:Name1:n5:alphai1e4:betai2ee");

        ObjectExtensionDataModel model = BencodeSerializer.Deserialize<ObjectExtensionDataModel>(bytes);

        Assert.AreEqual("n", model.Name);
        Assert.IsNotNull(model.Extra);
        Assert.AreEqual(2, model.Extra.Count);
        Assert.IsTrue(model.Extra.ContainsKey("alpha"));
        Assert.IsTrue(model.Extra.ContainsKey("beta"));
    }

    /// <summary>
    /// Verifies that a captured <see cref="BencodeObject" /> extension-data member is written back alongside the type's
    /// declared members, with all keys re-sorted into canonical ascending byte order.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenBencodeObjectExtensionData_ShouldWriteBackInCanonicalOrder()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d4:Name1:n5:alphai1e3:zzzi9ee");

        ObjectExtensionDataModel model = BencodeSerializer.Deserialize<ObjectExtensionDataModel>(bytes);
        byte[] rewritten = BencodeSerializer.Serialize(model);

        // Declared member Name and the two extension entries alpha and zzz merge into one sorted dictionary.
        Assert.AreEqual("d4:Name1:n5:alphai1e3:zzzi9ee", Encoding.Latin1.GetString(rewritten));
    }

    /// <summary>
    /// Verifies that unmatched dictionary entries are captured into a settable
    /// <c>IDictionary&lt;string, BencodeNode?&gt;</c> extension-data member on read.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDictionaryExtensionData_ShouldCaptureOverflow()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d4:Name1:n5:extrai7ee");

        DictionaryExtensionDataModel model = BencodeSerializer.Deserialize<DictionaryExtensionDataModel>(bytes);

        Assert.AreEqual("n", model.Name);
        Assert.IsNotNull(model.Extra);
        Assert.AreEqual(1, model.Extra.Count);
        Assert.IsTrue(model.Extra.ContainsKey("extra"));
    }

    /// <summary>
    /// Verifies that a get-only <c>IDictionary&lt;string, BencodeNode?&gt;</c> extension-data member, initialized by the
    /// type, is populated in place with the captured entries rather than being replaced.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenGetOnlyDictionaryExtensionData_ShouldPopulateExistingInstance()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d4:Name1:n5:extrai7ee");

        GetOnlyDictionaryExtensionDataModel model = BencodeSerializer.Deserialize<GetOnlyDictionaryExtensionDataModel>(bytes);

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
        byte[] bytes = Encoding.Latin1.GetBytes("d4:Name1:n5:extrai7ee");

        ObjectExtensionDataModel model = BencodeSerializer.Deserialize<ObjectExtensionDataModel>(bytes);

        Assert.AreEqual("n", model.Name);
        Assert.IsNotNull(model.Extra);
        Assert.IsFalse(model.Extra.ContainsKey("Name"));
        Assert.AreEqual(1, model.Extra.Count);
    }

    /// <summary>
    /// Verifies that a type with an extension-data member but no overflow entries leaves the member unset, so an empty
    /// document with only matched keys does not allocate extension data.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenNoUnmatchedKeys_ShouldLeaveExtensionDataUnset()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d4:Name1:ne");

        ObjectExtensionDataModel model = BencodeSerializer.Deserialize<ObjectExtensionDataModel>(bytes);

        Assert.AreEqual("n", model.Name);
        Assert.IsNull(model.Extra);
    }

    /// <summary>
    /// Verifies that an unmatched key is captured into extension data even when the serializer-wide unmapped-member
    /// handling is <see cref="BencodeUnmappedMemberHandling.Disallow" />, because extension data takes precedence.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDisallowAndExtensionDataPresent_ShouldCaptureAndNotThrow()
    {
        var options = new BencodeSerializerOptions { UnmappedMemberHandling = BencodeUnmappedMemberHandling.Disallow };
        byte[] bytes = Encoding.Latin1.GetBytes("d4:Name1:n7:unknowni1ee");

        ObjectExtensionDataModel model = BencodeSerializer.Deserialize<ObjectExtensionDataModel>(bytes, options);

        Assert.AreEqual("n", model.Name);
        Assert.IsNotNull(model.Extra);
        Assert.IsTrue(model.Extra.ContainsKey("unknown"));
    }

    /// <summary>
    /// Verifies that serializing a model whose extension-data member holds entries emits those entries merged with the
    /// declared members, exercising the write path directly from a constructed instance.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenExtensionDataPopulated_ShouldEmitMergedEntries()
    {
        var extra = new BencodeObject { ["zeta"] = BencodeValue.Create(1), ["alpha"] = BencodeValue.Create(2) };
        var model = new ObjectExtensionDataModel { Name = "n", Extra = extra };

        byte[] bytes = BencodeSerializer.Serialize(model);

        Assert.AreEqual("d4:Name1:n5:alphai2e4:zetai1ee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that resolving a type that declares more than one extension-data member throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenMultipleExtensionDataMembers_ShouldThrowInvalidOperationException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("de");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<MultipleExtensionDataModel>(bytes);
        });
    }

    /// <summary>
    /// Verifies that resolving a type whose extension-data member is of an unsupported type throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenExtensionDataMemberTypeUnsupported_ShouldThrowInvalidOperationException()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("de");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = BencodeSerializer.Deserialize<UnsupportedExtensionDataModel>(bytes);
        });
    }

    /// <summary>
    /// A model whose overflow keys are captured into a settable <see cref="BencodeObject" />.
    /// </summary>
    private sealed class ObjectExtensionDataModel
    {
        /// <summary>
        /// Gets or sets the declared name member.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the extension-data member that captures unmatched keys.
        /// </summary>
        /// <value>The captured entries, or <see langword="null" /> when none were read.</value>
        [BencodeExtensionData]
        public BencodeObject? Extra { get; set; }
    }

    /// <summary>
    /// A model whose overflow keys are captured into a settable dictionary.
    /// </summary>
    private sealed class DictionaryExtensionDataModel
    {
        /// <summary>
        /// Gets or sets the declared name member.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the extension-data member that captures unmatched keys.
        /// </summary>
        /// <value>The captured entries, or <see langword="null" /> when none were read.</value>
        [BencodeExtensionData]
        public IDictionary<string, BencodeNode?>? Extra { get; set; }
    }

    /// <summary>
    /// A model whose extension-data member is get-only and pre-initialized, populated in place on read.
    /// </summary>
    private sealed class GetOnlyDictionaryExtensionDataModel
    {
        /// <summary>
        /// Gets or sets the declared name member.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets the get-only extension-data member, populated in place with unmatched keys.
        /// </summary>
        /// <value>The captured entries.</value>
        [BencodeExtensionData]
        public IDictionary<string, BencodeNode?> Extra { get; } = new Dictionary<string, BencodeNode?>(StringComparer.Ordinal);
    }

    /// <summary>
    /// A model that declares two extension-data members, which is invalid.
    /// </summary>
    private sealed class MultipleExtensionDataModel
    {
        /// <summary>
        /// Gets or sets the first extension-data member.
        /// </summary>
        /// <value>The first captured entries.</value>
        [BencodeExtensionData]
        public BencodeObject? First { get; set; }

        /// <summary>
        /// Gets or sets the second extension-data member, which makes the type invalid.
        /// </summary>
        /// <value>The second captured entries.</value>
        [BencodeExtensionData]
        public BencodeObject? Second { get; set; }
    }

    /// <summary>
    /// A model whose extension-data member is of an unsupported type.
    /// </summary>
    private sealed class UnsupportedExtensionDataModel
    {
        /// <summary>
        /// Gets or sets the extension-data member declared with an unsupported value type.
        /// </summary>
        /// <value>The captured entries.</value>
        [BencodeExtensionData]
        public Dictionary<string, int>? Extra { get; set; }
    }
}
