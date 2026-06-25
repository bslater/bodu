// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.Serialize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies <see cref="YamlSerializer.Serialize{T}(T, YamlSerializerOptions?)" /> emission, including null-value
/// omission.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>Verifies that null members are omitted when the corresponding option is enabled.</summary>
    [TestMethod]
    public void Serialize_WhenIgnoreNullValues_ShouldOmit()
    {
        var options = new YamlSerializerOptions { IgnoreNullValues = true };
        var yaml = YamlSerializer.Serialize(new Person { Name = null, Age = 5, Active = true }, options);
        Assert.AreEqual("Age: 5\nActive: true\n", yaml);
    }
}
