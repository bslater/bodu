// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TweakableSymmetricAlgorithmExtensionsTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Extensions;

[TestClass]
public partial class TweakableSymmetricAlgorithmExtensionsTests
{
    private TweakableSymmetricAlgorithm CreateAlgorithm() => new SimpleReversingTweakableSymmetricAlgorithm();
}
