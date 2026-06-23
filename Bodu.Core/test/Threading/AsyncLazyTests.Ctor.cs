// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncLazyTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncLazyTests
{
    /// <summary>
    /// Verifies that a <see langword="null" /> value factory throws <see cref="ArgumentNullException" /> naming <c>valueFactory</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenValueFactoryIsNull_ShouldThrowForValueFactory()
    {
        Assert.AreEqual(
            "valueFactory",
            Assert.ThrowsExactly<ArgumentNullException>(() => new AsyncLazy<int>((Func<int>)null!)).ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> task factory throws <see cref="ArgumentNullException" /> naming <c>taskFactory</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenTaskFactoryIsNull_ShouldThrowForTaskFactory()
    {
        Assert.AreEqual(
            "taskFactory",
            Assert.ThrowsExactly<ArgumentNullException>(() => new AsyncLazy<int>((Func<Task<int>>)null!)).ParamName);
    }
}
