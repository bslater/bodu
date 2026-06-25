// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that a POCO survives a serialize-then-deserialize round trip with its values preserved.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>Verifies that a POCO round-trips through serialize and deserialize.</summary>
    [TestMethod]
    public void RoundTrip_WhenPoco_ShouldPreserveValues()
    {
        var original = new Person { Name = "Grace", Age = 45, Active = false };
        var yaml = YamlSerializer.Serialize(original);
        var restored = YamlSerializer.Deserialize<Person>(yaml)!;

        Assert.AreEqual("Grace", restored.Name);
        Assert.AreEqual(45, restored.Age);
        Assert.IsFalse(restored.Active);
    }
}
