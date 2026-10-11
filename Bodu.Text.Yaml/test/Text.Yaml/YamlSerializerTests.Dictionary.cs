// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.Dictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

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
