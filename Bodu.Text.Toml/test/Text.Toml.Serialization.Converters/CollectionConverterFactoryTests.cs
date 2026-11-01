// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CollectionConverterFactoryTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Verifies the type test of the shared <see cref="CollectionConverterFactory" /> on the types the serializer's
/// resolution order never asks it about, so that the factory stands on its own when consulted directly.
/// </summary>
[TestClass]
public class CollectionConverterFactoryTests
{
    /// <summary>
    /// Verifies that <see cref="string" />, although it enumerates characters, is not a collection to the factory, so
    /// a string is never written as an array of characters.
    /// </summary>
    [TestMethod]
    public void CanConvert_WhenTypeIsString_ShouldReturnFalse()
    {
        var factory = new CollectionConverterFactory();

        Assert.IsFalse(factory.CanConvert(typeof(string)));
    }

    /// <summary>
    /// Verifies that a list type is a collection to the factory, the positive counterpart of the string test.
    /// </summary>
    [TestMethod]
    public void CanConvert_WhenTypeIsList_ShouldReturnTrue()
    {
        var factory = new CollectionConverterFactory();

        Assert.IsTrue(factory.CanConvert(typeof(List<int>)));
    }
}
