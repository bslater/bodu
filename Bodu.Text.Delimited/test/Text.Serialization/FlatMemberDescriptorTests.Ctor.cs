// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatMemberDescriptorTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

public sealed partial class FlatMemberDescriptorTests
{
    /// <summary>
    /// Verifies that a read-only field is described as readable but not writable.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenFieldIsReadOnly_ShouldDescribeItAsReadableButNotWritable()
    {
        var descriptor = new DescriptorProbe(typeof(MemberSample).GetField(nameof(MemberSample.ImmutableField))!);

        Assert.IsTrue(descriptor.CanRead);
        Assert.IsFalse(descriptor.CanWrite);
    }

    /// <summary>
    /// Verifies that a property's order attribute is read.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPropertyHasOrderAttribute_ShouldReadTheOrder()
    {
        var descriptor = new DescriptorProbe(typeof(MemberSample).GetProperty(nameof(MemberSample.Number))!);

        Assert.AreEqual(2, descriptor.Order);
    }
}
