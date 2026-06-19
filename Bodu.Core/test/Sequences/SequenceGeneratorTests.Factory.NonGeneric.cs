// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGeneratorTests.Factory.NonGeneric.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Sequences;

public partial class SequenceGeneratorTests
{

    /// <summary>
    /// Verifies that the non-generic <see cref="IEnumerable.GetEnumerator" /> on a sequence returned from
    /// <see cref="SequenceGenerator.Factory{TResult}(Func{IEnumerator{TResult}})" /> yields the same elements as the strongly typed
    /// enumerator.
    /// </summary>
    [TestMethod]
    public void Factory_NonGenericGetEnumerator_ShouldYieldSameSequence()
    {
        IEnumerable<int> sequence = SequenceGenerator.Factory(() => new List<int> { 10, 20, 30 }.GetEnumerator());

        IEnumerable nonGeneric = sequence;
        var observed = new List<int>();
        foreach (object? item in nonGeneric)
            observed.Add((int)item);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, observed);
    }

}
