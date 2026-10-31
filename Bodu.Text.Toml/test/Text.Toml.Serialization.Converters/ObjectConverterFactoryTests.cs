// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObjectConverterFactoryTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Verifies the type test of the shared <see cref="ObjectConverterFactory" /> on the types the serializer's
/// resolution order never asks it about, so that the factory stands on its own when consulted directly.
/// </summary>
[TestClass]
public class ObjectConverterFactoryTests
{
    /// <summary>
    /// Verifies that a nullable value type is not an object to the factory, since the nullable converter unwraps it
    /// and the underlying type is converted on its own.
    /// </summary>
    [TestMethod]
    public void CanConvert_WhenTypeIsNullableValueType_ShouldReturnFalse()
    {
        var factory = new ObjectConverterFactory();

        Assert.IsFalse(factory.CanConvert(typeof(int?)));
    }

    /// <summary>
    /// Verifies that a plain struct is an object to the factory, the positive counterpart of the nullable test.
    /// </summary>
    [TestMethod]
    public void CanConvert_WhenTypeIsStruct_ShouldReturnTrue()
    {
        var factory = new ObjectConverterFactory();

        Assert.IsTrue(factory.CanConvert(typeof(KeyValuePair<string, int>)));
    }
}
