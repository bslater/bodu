// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGeneratorTests.Factory.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class SequenceGeneratorTests
{
    /// <summary>
    /// Verifies that <see cref="SequenceGenerator.Factory" /> returns the sequence produced by the supplied enumerator.
    /// </summary>
    [TestMethod]
    public void Factory_WhenEnumeratorIsValid_ShouldReturnExpectedSequence()
    {
        var actual = SequenceGenerator.Factory(() => new List<int> { 1, 2, 3 }.GetEnumerator()).ToArray();
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, actual);
    }

    /// <summary>
    /// Verifies that <see cref="SequenceGenerator.Factory" /> throws an ArgumentNullException when the enumerator factory is null.
    /// </summary>
    [TestMethod]
    public void Factory_WhenFactoryIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = SequenceGenerator.Factory<int>(null!).ToArray();
        });
    }

    /// <summary>
    /// Verifies that <see cref="SequenceGenerator.Factory" /> defers enumeration until the resulting sequence is explicitly consumed.
    /// </summary>
    [TestMethod]
    public void Factory_WhenCalled_ShouldDeferExecution()
    {
        var source = new List<int> { 1, 2, 3 };
        AssertExecutionIsDeferred("Factory", _ => SequenceGenerator.Factory(() => source.GetEnumerator()), source);
    }
}
