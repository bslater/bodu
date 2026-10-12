// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.SharedSources.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies that Bodu.Text.Yaml compiles only the shared serializer sources it uses, so that a shipping build, which
/// withdraws the test assemblies' access to its internals, raises no analyzer finding on code the package never runs.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>
    /// Verifies that the Bodu.Text.Yaml assembly holds none of the types the <c>shared/Flat</c> sources declare for the
    /// line formats, Bodu.Text.Delimited, Bodu.Text.DotEnv and Bodu.Text.Ini, none of which it uses.
    /// </summary>
    /// <param name="typeName">The full name of a type the <c>shared/Flat</c> sources declare.</param>
    [TestMethod]
    [DataRow("Bodu.Text.Serialization.FlatMemberDescriptor", DisplayName = "FlatMemberDescriptor")]
    [DataRow("Bodu.Text.Serialization.FlatMemberDiscovery", DisplayName = "FlatMemberDiscovery")]
    [DataRow("Bodu.Text.Serialization.InvariantScalarCodec", DisplayName = "InvariantScalarCodec")]
    [DataRow("Bodu.Text.Serialization.Utf8LineCursor", DisplayName = "Utf8LineCursor")]
    public void Assembly_WhenBuilt_ShouldNotCompileLineFormatSharedSources(string typeName)
    {
        Type? type = typeof(YamlSerializer).Assembly.GetType(typeName, throwOnError: false);

        Assert.IsNull(type, typeName);
    }
}
