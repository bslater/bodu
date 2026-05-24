// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICryptoTransformExtensionsTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

[TestClass]
public partial class ICryptoTransformExtensionsTests
{
    private SymmetricAlgorithm CreateAlgorithm() => new SimpleReversingSymmetricAlgorithm();
}
