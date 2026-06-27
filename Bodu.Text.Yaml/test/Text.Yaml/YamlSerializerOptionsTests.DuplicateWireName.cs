// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerOptionsTests.DuplicateWireName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that a type whose members resolve to the same YAML wire name is rejected by the serializer.
/// </summary>
public partial class YamlSerializerOptionsTests
{
    /// <summary>
    /// Verifies that a type mapping two members to the same YAML key is rejected with
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDuplicateWireName_ShouldThrow()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = YamlSerializer.Serialize(new Collision());
        });
    }
}
