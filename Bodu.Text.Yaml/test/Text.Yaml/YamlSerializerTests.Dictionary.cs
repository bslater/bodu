// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.Dictionary.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that <see cref="YamlSerializer" /> rejects dictionaries whose keys collapse to the same YAML key, so the
/// serializer never emits a document its own parser would reject as a duplicate key.
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
}
