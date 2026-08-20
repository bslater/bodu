// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstPropertyContextTests.IEnumerable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies the enumeration surface of <see cref="PstPropertyContext" />.
/// </summary>
public partial class PstPropertyContextTests
{
    /// <summary>
    /// Verifies that enumeration yields every value in property-identifier order with resolved payloads.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenEnumerated_ShouldYieldValuesInPropertyIdOrder()
    {
        (PstFile file, PstPropertyContext context) = OpenSharedContext();
        using (file)
        {
            var values = context.ToList();

            Assert.AreEqual(context.Count, values.Count);
            CollectionAssert.AreEqual(
                values.Select(static v => v.PropertyId).OrderBy(static id => id).ToArray(),
                values.Select(static v => v.PropertyId).ToArray());
            Assert.AreEqual("Sample1", values.Single(static v => v.PropertyId == StringId).GetString());
        }
    }

    /// <summary>
    /// Verifies that the non-generic enumerator yields the same values as the generic one.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenAccessedThroughIEnumerable_ShouldYieldSameValues()
    {
        (PstFile file, PstPropertyContext context) = OpenSharedContext();
        using (file)
        {
            int count = 0;
            foreach (object? item in (IEnumerable)context)
            {
                Assert.IsInstanceOfType<PstPropertyValue>(item);
                count++;
            }

            Assert.AreEqual(context.Count, count);
        }
    }
}
