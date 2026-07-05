// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.ValueTypes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that <see cref="TomlSerializer" /> preserves value-type semantics when a mutable struct is deserialized
/// through its settable members rather than a parameterized constructor.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that deserializing a mutable struct assigns its settable properties on the boxed instance so the
    /// values survive, rather than writing to a throwaway re-boxed copy and returning a default-valued struct.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenMutableStructWithSettableProperties_ShouldPreserveMemberValues()
    {
        var original = new MutablePoint { X = 3, Y = 7 };

        string toml = TomlSerializer.Serialize(original);
        MutablePoint result = TomlSerializer.Deserialize<MutablePoint>(toml);

        Assert.AreEqual(3, result.X);
        Assert.AreEqual(7, result.Y);
    }

    /// <summary>
    /// A mutable value type deserialized through its settable properties (no parameterized constructor).
    /// </summary>
    public struct MutablePoint
    {
        /// <summary>Gets or sets the X coordinate.</summary>
        /// <value>The X coordinate.</value>
        public int X { get; set; }

        /// <summary>Gets or sets the Y coordinate.</summary>
        /// <value>The Y coordinate.</value>
        public int Y { get; set; }
    }
}
