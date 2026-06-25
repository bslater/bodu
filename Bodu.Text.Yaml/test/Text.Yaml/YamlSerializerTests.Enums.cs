// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.Enums.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies enum serialization and deserialization defaults.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>Verifies that enums serialize as names by default and parse back.</summary>
    [TestMethod]
    public void RoundTrip_WhenEnum_ShouldUseNames()
    {
        var yaml = YamlSerializer.Serialize(Color.Green);
        Assert.AreEqual("Green\n", yaml);
        Assert.AreEqual(Color.Green, YamlSerializer.Deserialize<Color>(yaml));
    }
}
