// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Serialization;

using Bodu.Text.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the BCL-aligned behaviors added to <see cref="YamlSerializerOptions" /> and the serializer: freeze-on-use,
/// number handling, unmapped-member handling, merge-key behavior, and duplicate wire-name detection. Test methods
/// live in the option-specific partial files; this root holds the shared target fixtures.
/// </summary>
[TestClass]
public partial class YamlSerializerOptionsTests
{
    /// <summary>A simple target type with a single mapped member.</summary>
    private sealed class Point
    {
        /// <summary>Gets or sets the x coordinate.</summary>
        public int X { get; set; }
    }

    /// <summary>A type whose explicit names collide on a single YAML key.</summary>
    private sealed class Collision
    {
        /// <summary>Gets or sets the first colliding member.</summary>
        [PropertyName("k")]
        public int A { get; set; }

        /// <summary>Gets or sets the second colliding member.</summary>
        [PropertyName("k")]
        public int B { get; set; }
    }
}
