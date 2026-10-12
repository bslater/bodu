// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.Dictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using Bodu.Text.Serialization;
using Bodu.Text.Yaml.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the dictionary surface of <see cref="YamlSerializer" />: dictionaries whose keys collapse to the same YAML
/// key are rejected, so the serializer never emits a document its own parser would reject as a duplicate key, and a
/// dictionary whose keys are not strings round-trips, its keys parsed back.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>
    /// Verifies that distinct dictionary keys whose string forms collide are rejected with
    /// <see cref="YamlSerializationException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDictionaryKeysCollapseToSameYamlKey_ShouldThrowYamlSerializationException()
    {
        var value = new Hashtable
        {
            [1] = "numeric",
            ["1"] = "string",
        };

        Assert.ThrowsExactly<YamlSerializationException>(() => YamlSerializer.Serialize(value));
    }

    /// <summary>
    /// Verifies that a dictionary with distinct, non-colliding keys serializes without error.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDictionaryKeysAreDistinct_ShouldSucceed()
    {
        var value = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };

        string yaml = YamlSerializer.Serialize(value);

        Assert.AreEqual("a: 1\nb: 2\n", yaml);
    }

    /// <summary>
    /// Verifies that a dictionary member with <see cref="int" /> keys and struct values round-trips: each key is written
    /// as a mapping key and parsed back to the integer, and each struct value is restored.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenDictionaryHasIntegerKeysAndStructValues_ShouldRoundTrip()
    {
        var original = new NestedDictionaryModel();
        original.Lookups[1] = new NestedDictionaryModel.Payload { I = 1 };

        string yaml = YamlSerializer.Serialize(original);
        NestedDictionaryModel restored = YamlSerializer.Deserialize<NestedDictionaryModel>(yaml)!;

        Assert.AreEqual(1, restored.Lookups[1].I, yaml);
    }

    /// <summary>
    /// Verifies that an enumeration key is written by its wire name, as System.Text.Json writes it: a member renamed with
    /// <see cref="StringEnumMemberNameAttribute" /> by its configured name, in insertion order.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenEnumKeyIsRenamed_ShouldWriteConfiguredName()
    {
        var value = new Dictionary<KeyToggle, int> { [KeyToggle.Enabled] = 1, [KeyToggle.Disabled] = 2 };

        string yaml = YamlSerializer.Serialize(value);

        Assert.AreEqual("active: 1\nDisabled: 2\n", yaml);
    }

    /// <summary>
    /// Verifies that, under a <see cref="YamlStringEnumConverter" /> with a naming policy, an enumeration key and a
    /// combination of flags used as a key are written with the policy's names, as System.Text.Json writes them.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenStringEnumConverterHasNamingPolicy_ShouldWriteKeysWithPolicyNames()
    {
        var value = new Dictionary<KeyAccess, int> { [KeyAccess.CanRead | KeyAccess.CanExecute] = 1, [KeyAccess.CanWrite] = 2 };

        string yaml = YamlSerializer.Serialize(value, SnakeCaseKeyOptions(allowIntegerValues: true));

        CollectionAssert.AreEqual(
            new[] { "can_read, can_execute", "can_write" },
            YamlSerializer.Deserialize<Dictionary<string, int>>(yaml)!.Keys.ToArray(),
            yaml);
    }

    /// <summary>
    /// Verifies that an enumeration key whose value is neither a member nor a combination of defined flags is written
    /// as its decimal text, a string key that reads back to the same value.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenEnumKeyIsUndefinedValue_ShouldWriteDecimalText()
    {
        var value = new Dictionary<KeyToggle, int> { [(KeyToggle)7] = 1 };

        string yaml = YamlSerializer.Serialize(value);

        CollectionAssert.AreEqual(new[] { "7" }, YamlSerializer.Deserialize<Dictionary<string, int>>(yaml)!.Keys.ToArray(), yaml);
        Assert.AreEqual(1, YamlSerializer.Deserialize<Dictionary<KeyToggle, int>>(yaml)![(KeyToggle)7], yaml);
    }

    /// <summary>
    /// Verifies that a mapping key holding a renamed member's configured name binds to that member.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenEnumKeyIsConfiguredName_ShouldBindRenamedMember()
    {
        Dictionary<KeyToggle, int> value = YamlSerializer.Deserialize<Dictionary<KeyToggle, int>>("active: 1\n")!;

        Assert.AreEqual(1, value[KeyToggle.Enabled]);
    }

    /// <summary>
    /// Verifies that a mapping key holding the CLR name of a member renamed with
    /// <see cref="StringEnumMemberNameAttribute" /> throws <see cref="YamlSerializationException" />, as
    /// System.Text.Json rejects it.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenEnumKeyIsRenamedMembersClrName_ShouldThrowYamlSerializationException()
    {
        _ = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<Dictionary<KeyToggle, int>>("Enabled: 1\n");
        });
    }

    /// <summary>
    /// Verifies that a mapping key that names no member of the enumeration throws
    /// <see cref="YamlSerializationException" /> naming the key.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenEnumKeyNamesNoMember_ShouldThrowYamlSerializationException()
    {
        YamlSerializationException ex = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<Dictionary<KeyToggle, int>>("Purple: 1\n");
        });

        Assert.IsTrue(ex.Message.Contains("'Purple'", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that a mapping key holding integer text binds to the value it names under the default enumeration
    /// handling, and that an unrenamed member's CLR name binds in any case.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenEnumKeyIsIntegerTextOrClrNameInAnotherCase_ShouldBind()
    {
        Dictionary<KeyToggle, int> value = YamlSerializer.Deserialize<Dictionary<KeyToggle, int>>("1: 1\ndisabled: 2\n")!;

        Assert.AreEqual(1, value[KeyToggle.Enabled]);
        Assert.AreEqual(2, value[KeyToggle.Disabled]);
    }

    /// <summary>
    /// Verifies that, under a <see cref="YamlStringEnumConverter" /> with a naming policy, a mapping key holding the
    /// policy's names of a combination of flags binds to that combination.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStringEnumConverterHasNamingPolicy_ShouldBindKeyPolicyNames()
    {
        Dictionary<KeyAccess, int> value = YamlSerializer.Deserialize<Dictionary<KeyAccess, int>>(
            "\"can_execute, can_read\": 1\n", SnakeCaseKeyOptions(allowIntegerValues: true))!;

        Assert.AreEqual(1, value[KeyAccess.CanRead | KeyAccess.CanExecute]);
    }

    /// <summary>
    /// Verifies that, under a <see cref="YamlStringEnumConverter" /> that disallows integer values, a mapping key
    /// holding integer text throws <see cref="YamlSerializationException" />, as System.Text.Json rejects it.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStringEnumConverterDisallowsIntegersAndKeyIsIntegerText_ShouldThrowYamlSerializationException()
    {
        var options = new YamlSerializerOptions();
        options.Converters.Add(new YamlStringEnumConverter(namingPolicy: null, allowIntegerValues: false));

        _ = Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<Dictionary<KeyToggle, int>>("1: 1\n", options);
        });
    }

    /// <summary>
    /// Creates options that convert enumerations by name with the lower snake-case naming policy.
    /// </summary>
    /// <param name="allowIntegerValues">Whether integer values are allowed.</param>
    /// <returns>The options.</returns>
    private static YamlSerializerOptions SnakeCaseKeyOptions(bool allowIntegerValues)
    {
        var options = new YamlSerializerOptions();
        options.Converters.Add(new YamlStringEnumConverter(NamingPolicy.SnakeCaseLower, allowIntegerValues));
        return options;
    }

    /// <summary>
    /// An enumeration used as a dictionary key, one of whose members carries a configured name.
    /// </summary>
    private enum KeyToggle
    {
        /// <summary>
        /// The disabled state.
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// The enabled state, written under the name <c>active</c>.
        /// </summary>
        [StringEnumMemberName("active")]
        Enabled = 1,
    }

    /// <summary>
    /// A flags enumeration used as a dictionary key, whose members' names change under a naming policy.
    /// </summary>
    [Flags]
    private enum KeyAccess
    {
        /// <summary>
        /// No access.
        /// </summary>
        None = 0,

        /// <summary>
        /// Read access.
        /// </summary>
        CanRead = 1,

        /// <summary>
        /// Write access.
        /// </summary>
        CanWrite = 2,

        /// <summary>
        /// Execute access.
        /// </summary>
        CanExecute = 4,
    }

    /// <summary>A model holding a dictionary with integer keys and struct values.</summary>
    private sealed class NestedDictionaryModel
    {
        /// <summary>Gets or sets the payloads, keyed by an integer.</summary>
        /// <value>The payloads.</value>
        public Dictionary<int, Payload> Lookups { get; set; } = new();

        /// <summary>A struct value the dictionary holds.</summary>
        public struct Payload
        {
            /// <summary>Gets or sets the payload value.</summary>
            /// <value>The payload value.</value>
            public int I { get; set; }
        }
    }
}
