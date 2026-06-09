// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationViewTests.NonGenericEnumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Text.Configuration;

public partial class ConfigurationViewTests
{
    /// <summary>
    /// Verifies that the explicit non-generic <see cref="IEnumerable.GetEnumerator" /> yields the resolved entries.
    /// </summary>
    [TestMethod]
    public void NonGenericGetEnumerator_ShouldYieldResolvedEntries()
    {
        ConfigurationView view = ConfigurationDocument.Parse("[*]\na = 1\n").Resolve("any.cs");

        IEnumerator enumerator = ((IEnumerable)view).GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
    }
}
