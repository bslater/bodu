// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageBuilderSerializationTests.Guards.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.Builders;

namespace Bodu.IO.Compound;

public partial class CompoundStorageBuilderSerializationTests
{
    /// <summary>
    /// Verifies that exceeding the configured maximum nesting depth throws
    /// <see cref="CompoundFileSerializationException" /> during serialization.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenNestingExceedsMaxDepth_ShouldThrowCompoundFileSerializationException()
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStorage("A").AddStorage("B").AddStorage("C");

        _ = Assert.ThrowsExactly<CompoundFileSerializationException>(
            () => builder.ToArray(new CompoundBuildOptions { MaxDepth = 2 }));
    }
}
