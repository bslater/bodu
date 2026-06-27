// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the <see cref="YamlSerializer" /> POCO mapper. Test methods live in the member- and subject-specific
/// partial files (serialize, deserialize, round-trip, coercion, dictionary, diagnostics); this root holds the
/// shared POCO fixtures.
/// </summary>
[TestClass]
public partial class YamlSerializerTests
{
    /// <summary>A simple POCO used across the serializer tests.</summary>
    private sealed class Person
    {
        public string? Name { get; set; }

        public int Age { get; set; }

        public bool Active { get; set; }
    }

    /// <summary>A POCO exercising naming policy and the property-name and ignore attributes.</summary>
    private sealed class Config
    {
        public string? ServerHost { get; set; }

        [YamlPropertyName("port")]
        public int ServerPort { get; set; }

        [YamlIgnore]
        public string? Secret { get; set; }
    }

    /// <summary>A POCO with nested collections.</summary>
    private sealed class Project
    {
        public string? Title { get; set; }

        public List<string>? Tags { get; set; }

        public Dictionary<string, int>? Counts { get; set; }
    }

    /// <summary>An enumeration used to test enum serialization.</summary>
    private enum Color
    {
        Red,
        Green,
        Blue,
    }
}
