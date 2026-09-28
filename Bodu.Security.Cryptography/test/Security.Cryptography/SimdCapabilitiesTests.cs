// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimdCapabilitiesTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="SimdCapabilities" />, the gates that choose between the vectorized and scalar code paths,
/// grouped into member-named partial files. The gates' behavior with the opt-out switch set is covered by
/// <c>Bodu.Security.Cryptography.Simd.Test</c>, which runs with the switch on.
/// </summary>
[TestClass]
public sealed partial class SimdCapabilitiesTests
{
}
