// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatBindingTests.CreateInstance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

public sealed partial class FlatBindingTests
{
    /// <summary>
    /// Verifies that a value type and a class with a public parameterless constructor are created.
    /// </summary>
    [TestMethod]
    public void CreateInstance_WhenTheTypeCanBeCreated_ShouldCreateIt()
    {
        Assert.IsInstanceOfType<ValueSample>(FlatBinding.CreateInstance(typeof(ValueSample), CreateNotCreatableException));
        Assert.IsInstanceOfType<CreatableSample>(FlatBinding.CreateInstance(typeof(CreatableSample), CreateNotCreatableException));
    }

    /// <summary>
    /// Verifies that an abstract class, an interface and a class without a public parameterless constructor throw the
    /// caller's exception, naming the type.
    /// </summary>
    /// <param name="type">The type that cannot be created.</param>
    [TestMethod]
    [DataRow(typeof(AbstractSample))]
    [DataRow(typeof(IDisposable))]
    [DataRow(typeof(ConstructedSample))]
    public void CreateInstance_WhenTheTypeCannotBeCreated_ShouldThrowTheCallersException(Type type)
    {
        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = FlatBinding.CreateInstance(type, CreateNotCreatableException);
        });

        Assert.AreEqual(type.Name, ex.Message);
    }
}
