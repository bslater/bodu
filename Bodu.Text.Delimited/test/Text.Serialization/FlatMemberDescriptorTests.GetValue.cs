// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatMemberDescriptorTests.GetValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

public sealed partial class FlatMemberDescriptorTests
{
    /// <summary>
    /// Verifies that a field's value is read from the instance it belongs to.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenMemberIsField_ShouldReturnItsValue()
    {
        var descriptor = new DescriptorProbe(typeof(MemberSample).GetField(nameof(MemberSample.ImmutableField))!);

        Assert.AreEqual(12, descriptor.GetValue(new MemberSample()));
    }
}
