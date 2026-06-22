// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileBuilderTests.Guards.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.Builders;

namespace Bodu.IO.Compound;

public partial class CompoundFileBuilderTests
{
    /// <summary>
    /// Verifies that exceeding the configured maximum nesting depth throws
    /// <see cref="CompoundFileSerializationException" /> during serialization.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenNestingExceedsMaxDepth_ShouldThrowCompoundFileSerializationException()
    {
        var builder = new CompoundFileBuilder(new CompoundBuildOptions { MaxDepth = 2 });
        _ = builder.Root.AddStorage("A").AddStorage("B").AddStorage("C");

        _ = Assert.ThrowsExactly<CompoundFileSerializationException>(() => builder.ToArray());
    }
}
