// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlValueTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Nodes;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the mutable <see cref="YamlValue" /> node, including its typed value conversion behavior.
/// </summary>
[TestClass]
public sealed class YamlValueTests
{
    /// <summary>
    /// Verifies that <see cref="YamlValue.GetValue{T}" /> wraps a failed conversion in an
    /// <see cref="InvalidOperationException" /> that carries the original cause.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenConversionFails_ShouldThrowWithInnerException()
    {
        var value = YamlValue.Create("not-a-number");

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() => value.GetValue<int>());
        Assert.IsNotNull(ex.InnerException);
    }
}
