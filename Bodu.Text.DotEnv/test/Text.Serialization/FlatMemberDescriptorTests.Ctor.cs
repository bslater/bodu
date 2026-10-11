// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatMemberDescriptorTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

public sealed partial class FlatMemberDescriptorTests
{
    /// <summary>
    /// Verifies that a property's name and required attributes are read, and that a public property with public
    /// accessors is readable and writable.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPropertyHasNameAndRequiredAttributes_ShouldReadThem()
    {
        var descriptor = new DescriptorProbe(typeof(MemberSample).GetProperty(nameof(MemberSample.Count))!);

        Assert.AreEqual("CUSTOM_KEY", descriptor.Name);
        Assert.IsTrue(descriptor.Required);
        Assert.IsTrue(descriptor.CanRead);
        Assert.IsTrue(descriptor.CanWrite);
    }
}
