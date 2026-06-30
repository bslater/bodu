// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlValueTests.GetValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Nodes;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies <see cref="YamlValue.GetValue{T}" />.
/// </summary>
public partial class YamlValueTests
{
    /// <summary>
    /// Verifies that <see cref="YamlValue.GetValue{T}" /> wraps a failed conversion in an
    /// <see cref="InvalidOperationException" /> that carries the original cause.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenConversionFails_ShouldThrowWithInnerException()
    {
        var value = YamlValue.Create("not-a-number");

        InvalidOperationException ex = Assert.ThrowsExactly<InvalidOperationException>(() => value.GetValue<int>());
        Assert.IsNotNull(ex.InnerException);
    }
}
